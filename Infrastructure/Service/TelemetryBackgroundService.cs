using Application.DTOs;
using Application.Interface;
using Domain.Entities;
using Infrastructure.DBs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public int BucketSeconds { get; set; } = 60;
        public int FlushIntervalMs { get; set; } = 5000;
        public int MaxHistoryBuckets { get; set; } = 100; // for graphing
    }

    public class AggregatedRow
    {
        public Guid AssetId { get; set; }
        public Guid SignalTypeId { get; set; }
        public Guid DeviceId { get; set; }
        public Guid DevicePortId { get; set; }
        public string SignalName { get; set; }
        public string SignalUnit { get; set; }
        public int? RegisterAddress { get; set; }
        public DateTime BucketStartUtc { get; set; }
        public int Count { get; set; }
        public double Sum { get; set; }
        public double MinValue { get; set; }
        public double MaxValue { get; set; }
        public double Avg => Count > 0 ? Sum / Count : 0;
    }

    public class TelemetryBackgroundService : BackgroundService
    {
        private readonly ILogger<TelemetryBackgroundService> _logger;
        private readonly IMappingCache _mappingCache;
        private readonly TelemetryOptions _options;

        private IConnection _connection;
        private IModel _channel;
        private EventingBasicConsumer _consumer;

        // Aggregates per bucket/signal/register/device/port/asset
        private ConcurrentDictionary<(long BucketTicks, Guid AssetId, Guid DeviceId, Guid PortId, int? Register, Guid SignalTypeId), AggregatedRow> _aggregates
            = new();

        // Rolling history per signal/register for graphing
        private readonly ConcurrentDictionary<(Guid AssetId, Guid DeviceId, Guid PortId, int? Register, Guid SignalTypeId), LinkedList<AggregatedRow>> _history
            = new();

        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public TelemetryBackgroundService(
            ILogger<TelemetryBackgroundService> logger,
            IMappingCache mappingCache,
            IOptions<TelemetryOptions> options)
        {
            _logger = logger;
            _mappingCache = mappingCache;
            _options = options.Value;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
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
            _consumer.Received += OnReceived;
            _channel.BasicConsume(queue: _options.Queue, autoAck: false, consumer: _consumer);

            _logger.LogInformation("TelemetryBackgroundService started consuming queue {Queue}", _options.Queue);
            return base.StartAsync(cancellationToken);
        }

        private async void OnReceived(object? sender, BasicDeliverEventArgs ea)
        {
            await Task.Yield();
            //ensures execution continues on thread pool asynchronously (avoids doing heavy work on RabbitMQ client's I/O thread).

            try
            {
                var body = ea.Body.ToArray();
                TelemetryDto dto;
                try
                {
                    dto = JsonSerializer.Deserialize<TelemetryDto>(body, _jsonOptions);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize telemetry message; acking to drop");
                    _channel.BasicAck(ea.DeliveryTag, false);
                    return;
                }

                if (dto == null)
                {
                    _channel.BasicAck(ea.DeliveryTag, false);
                    return;
                }

                if (!_mappingCache.TryGet(dto.DeviceId, dto.deviceSlaveId, out var mapping))
                {
                    _logger.LogDebug("Telemetry unmapped for Device:{Device} Port:{Port}", dto.DeviceId, dto.deviceSlaveId);
                    _channel.BasicAck(ea.DeliveryTag, false);
                    return;
                }

                // Align timestamp to bucket
                var bucketStart = AlignToBucket(dto.TimestampUtc, TimeSpan.FromSeconds(_options.BucketSeconds));

                // Key uniquely identifies each signal/register/port/device/asset/bucket
                var key = (
                    BucketTicks: bucketStart.Ticks,
                    AssetId: mapping.AssetId,
                    DeviceId: dto.DeviceId,
                    PortId: dto.deviceSlaveId,
                    Register: dto.RegisterAddress,
                    SignalTypeId: mapping.SignalTypeId
                );

                _aggregates.AddOrUpdate(
                    key,
                    addValueFactory: k => new AggregatedRow
                    {
                        AssetId = mapping.AssetId,
                        SignalTypeId = mapping.SignalTypeId,
                        DeviceId = dto.DeviceId,
                        DevicePortId = dto.deviceSlaveId,
                        SignalName = mapping.SignalName,
                        SignalUnit = dto.Unit ?? mapping.SignalUnit,
                        RegisterAddress = dto.RegisterAddress,
                        BucketStartUtc = bucketStart,
                        Count = 1,
                        Sum = dto.Value,
                        MinValue = dto.Value,
                        MaxValue = dto.Value
                    },
                    updateValueFactory: (_, cur) =>
                    {
                        cur.Count += 1;
                        cur.Sum += dto.Value;
                        cur.MinValue = Math.Min(cur.MinValue, dto.Value);
                        cur.MaxValue = Math.Max(cur.MaxValue, dto.Value);
                        return cur;
                    });
                // agar bucket nahi hai, to  new AggregatedRow-> new bucket create 
                //  updateValueFactory: -> vahi bucket me aggregated hoga 

                _channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing telemetry message; acking to avoid poison queue");
                try { _channel.BasicAck(ea.DeliveryTag, false); } catch { }
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_options.FlushIntervalMs, stoppingToken);

                    var snapshot = Interlocked.Exchange(
                        ref _aggregates,
                        new ConcurrentDictionary<(long, Guid, Guid, Guid, int?, Guid), AggregatedRow>()
                    );

                    if (snapshot == null || snapshot.Count == 0)
                        continue;

                    var rows = snapshot.Values.ToList();

                    foreach (var row in rows)
                    {
                        // Store rolling history for plotting
                        var histKey = (row.AssetId, row.DeviceId, row.DevicePortId, row.RegisterAddress, row.SignalTypeId);
                        var list = _history.GetOrAdd(histKey, _ => new LinkedList<AggregatedRow>());
                        list.AddLast(row);
                        while (list.Count > _options.MaxHistoryBuckets)
                            list.RemoveFirst();

                        // Output for debugging / logging
                        Console.WriteLine(
                            $"AssetId:       {row.AssetId}\n" +
                            $"SignalTypeId:  {row.SignalTypeId}\n" +
                            $"DeviceId:      {row.DeviceId}\n" +
                            $"DevicePortId:  {row.DevicePortId}\n" +
                            $"Signal:        {row.SignalName} ({row.SignalUnit})\n" +
                            $"Register:      {row.RegisterAddress}\n" +
                            $"Bucket:        {row.BucketStartUtc}\n" +
                            $"Count:         {row.Count}\n" +
                            $"Sum:           {row.Sum}\n" +
                            $"Min:           {row.MinValue}\n" +
                            $"Max:           {row.MaxValue}\n" +
                            $"Avg:           {row.Avg}\n" +
                            $"--------------------------------------------"
                        );
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "FlusherLoop error - incoming aggregates may be lost");
                }
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            try { _channel?.Close(); _channel?.Dispose(); } catch { }
            try { _connection?.Close(); _connection?.Dispose(); } catch { }
            return base.StopAsync(cancellationToken);
        }

        private static DateTime AlignToBucket(DateTime utc, TimeSpan bucket)
        {
            var ticks = utc.Ticks - (utc.Ticks % bucket.Ticks);
            return new DateTime(ticks, DateTimeKind.Utc);
        }

        // Optional helper to get rolling history for plotting
        public IReadOnlyList<AggregatedRow> GetHistory(Guid assetId, Guid deviceId, Guid portId, int? register, Guid signalTypeId)
        {
            var key = (assetId, deviceId, portId, register, signalTypeId);
            if (_history.TryGetValue(key, out var list))
                return list.ToList();
            return Array.Empty<AggregatedRow>();
        }
    }
}
