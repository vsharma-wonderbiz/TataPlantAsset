using Application.DTOs;
using Application.Interface;
using Infrastructure.DBs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Service
{
    public class TelemetryOptions
    {
        public string RabbitHost { get; set; } = "localhost";
        public string RabbitUser { get; set; } = "guest";
        public string RabbitPass { get; set; } = "guest";
        public string Queue { get; set; } = "telemetry_queue";
        public ushort Prefetch { get; set; } = 200;
    }

    public class TelemetryBackgroundService : BackgroundService
    {
        private readonly ILogger<TelemetryBackgroundService> _logger;
        private readonly IMappingCache _mappingCache;
        private readonly TelemetryOptions _options;
        private readonly IServiceProvider _serviceProvider;

        private IConnection _connection;
        private IModel _channel;
        private EventingBasicConsumer _consumer;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
        private readonly IAlertStateStore _alertStore;

        public TelemetryBackgroundService(
            ILogger<TelemetryBackgroundService> logger,
            IMappingCache mappingCache,
            IOptions<TelemetryOptions> options,
            IServiceProvider serviceProvider,
            IAlertStateStore alertStore)
        {
            _logger = logger;
            _mappingCache = mappingCache;
            _options = options.Value;
            _serviceProvider = serviceProvider;
            _alertStore = alertStore;
        }

        private object BuildNotificationPayload(
            string assetName, string signalName, double value, double min, double max)
        {
            string statusType;
            double percent;

            if (value < min)
            {
                percent = ((min - value) / min) * 100;
                statusType = "LOW";
            }
            else if (value > max)
            {
                percent = ((value - max) / max) * 100;
                statusType = "HIGH";
            }
            else
            {
                return null;
            }

            return new
            {
                asset = assetName,
                signal = signalName,
                value = value,
                min = min,
                max = max,
                status = statusType,
                percent = Math.Round(percent, 1),
                timestamp = DateTime.UtcNow.ToString("o")  // ISO format
            };
        }


        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Waiting for InfluxDB to initialize...");
            await Task.Delay(3000, cancellationToken);

            var factory = new ConnectionFactory
            {
                HostName = _options.RabbitHost,
                UserName = _options.RabbitUser,
                Password = _options.RabbitPass,
                AutomaticRecoveryEnabled = true
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.BasicQos(0, _options.Prefetch, false);

            _consumer = new EventingBasicConsumer(_channel);
            _consumer.Received += async (sender, ea) => await OnReceivedAsync(ea);
            _channel.BasicConsume(queue: _options.Queue, autoAck: false, consumer: _consumer);

            _logger.LogInformation("TelemetryBackgroundService started consuming queue {Queue}", _options.Queue);
            await base.StartAsync(cancellationToken);
        }

        private async Task OnReceivedAsync(BasicDeliverEventArgs ea)
        {
            await Task.Yield(); // ensure async context

            try
            {
                var body = ea.Body.ToArray();
                TelemetryDto dto;

                try
                {
                    dto = JsonSerializer.Deserialize<TelemetryDto>(body, _jsonOptions);
                    if (dto == null) throw new Exception("Telemetry DTO is null.");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize telemetry message; acking to drop");
                    _channel.BasicAck(ea.DeliveryTag, false);
                    return;
                }

                if (!_mappingCache.TryGet(dto.DeviceId, dto.deviceSlaveId, out var mapping))
                {
                    _logger.LogDebug("Telemetry unmapped for Device:{Device} Port:{Port}", dto.DeviceId, dto.deviceSlaveId);
                    _channel.BasicAck(ea.DeliveryTag, false);
                    return;
                }

                var influxDto = new InfluxTelementryDto
                {
                    AssetId = mapping.AssetId,
                    DeviceId = dto.DeviceId,
                    deviceSlaveId = dto.deviceSlaveId,
                    SignalTypeId = mapping.SignalTypeId,
                    MappingId = mapping.MappingId,
                    RegisterAddress = dto.RegisterAddress,
                    SignalType = mapping.SignalName,
                    Value = dto.Value,
                    Unit = dto.Unit,
                    Timestamp = dto.TimestampUtc
                };

                using var scope = _serviceProvider.CreateScope();
                var influxService = scope.ServiceProvider.GetRequiredService<IInfluxTelementryService>();
                await influxService.WriteTelemetryAsync(influxDto);

                var assetService = scope.ServiceProvider.GetRequiredService<IAssetHierarchyService>();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                // Get asset name
                var assetName = await assetService.GetAssetNameAsync(mapping.AssetId) ?? "Unknown Asset";

                //Console.WriteLine(mapping.SignalTypeId);
                // Get signal thresholds
                var signal = await assetService.GetSignalTypeAsync(mapping.SignalTypeId);
              
                if (signal != null && influxDto.SignalTypeId == signal.SignalTypeID && influxDto.RegisterAddress == signal.DefaultRegisterAdress )
                {
                    // after you already have `signal` and `influxDto`:
                    var now = DateTime.UtcNow;
                    var mappingKey = mapping.MappingId; // use MappingId as unique key

                    bool isOutOfRange = influxDto.Value < signal.MinThreshold || influxDto.Value > signal.MaxThreshold;

                    if (isOutOfRange)
                    {
                        // check if there is already an active alert
                        var current = await _alertStore.GetAsync(mappingKey);
                        if (current == null || !current.IsActive)
                        {
                            // start alert
                            await _alertStore.SetActiveAsync(mappingKey, now, influxDto.Value);

                            var startPayload = BuildNotificationPayload(
                                assetName,
                                signal.SignalName,
                                influxDto.Value,
                                signal.MinThreshold,
                                signal.MaxThreshold
                            );

                            var notificationRequest = new NotificationCreateRequest(
                                Title: $"Alert START: {signal.SignalName} exceeded",
                                Text: JsonSerializer.Serialize(startPayload),
                                ExpiresAt: null,
                                Priority: 0
                            );

                            await notificationService.CreateForUsersAsync(notificationRequest);
                            _logger.LogInformation("Sent START notification for {Asset} {Signal}", assetName, signal.SignalName);
                        }
                        else
                        {
                            // already active — update stats only
                            await _alertStore.UpdateActiveAsync(mappingKey, influxDto.Value, now);
                            // optionally log/debug
                        }
                    }
                    else
                    {
                        // value back to normal — if active then clear and send resolved notification
                        var active = await _alertStore.GetAsync(mappingKey);
                        if (active != null && active.IsActive)
                        {
                            var saved = await _alertStore.ClearActiveAsync(mappingKey, now);

                            if (saved != null)
                            {
                                var duration = now - saved.StartUtc;
                                var resolvedPayload = new
                                {
                                    asset = assetName,
                                    signal = signal.SignalName,
                                    from = saved.StartUtc.ToString("o"),
                                    to = now.ToString("o"),
                                    durationSeconds = (int)duration.TotalSeconds,
                                    min = saved.MinValue,
                                    max = saved.MaxValue
                                };

                                var notificationRequest = new NotificationCreateRequest(
                                    Title: $"Alert RESOLVED: {signal.SignalName} normalised",
                                    Text: JsonSerializer.Serialize(resolvedPayload),
                                    ExpiresAt: null,
                                    Priority: 0
                                );

                                await notificationService.CreateForUsersAsync(notificationRequest);
                                _logger.LogInformation("Sent RESOLVED notification for {Asset} {Signal}", assetName, signal.SignalName);
                            }
                        }
                    }

                }

                //_logger.LogInformation("Telemetry written to InfluxDB for Device:{Device} Signal:{Signal}", dto.DeviceId, mapping.SignalName);
                _channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing telemetry message; acking to avoid poison queue");
                try { _channel.BasicAck(ea.DeliveryTag, false); } catch { }
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            try { _channel?.Close(); _channel?.Dispose(); } catch { }
            try { _connection?.Close(); _connection?.Dispose(); } catch { }
            _logger.LogInformation("TelemetryBackgroundService stopped.");
            return base.StopAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
    }
}
