using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Enums;
using Application.Interface;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using Infrastructure.Configuration;
using Infrastructure.DBs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;

namespace Infrastructure.Service
{
    public class InfluxTelemetryService : IInfluxTelementryService
    {
        private readonly InfluxDBClient _client;
        private readonly string _bucket;
        private readonly string _org;
        private readonly DBContext _dbContext;
        private readonly QueryApi _queryApi;

        public InfluxTelemetryService(
            IInfluxDbConnectionService client,
            IOptions<InfluxDbOptions> options,
            DBContext dbContext)
        {
            _client = client.GetClient();
            var config = options.Value;
            _bucket = config.InfluxBucket;
            _org = config.InfluxOrg;
            _dbContext = dbContext;
            _queryApi = _client.GetQueryApi();
        }

        public async Task WriteTelemetryAsync(InfluxTelementryDto dto)
        {
            try
            {
                var point = PointData
                    .Measurement("signals")
                    .Tag("assetId", dto.AssetId.ToString())
                    .Tag("signalTypeId", dto.SignalTypeId.ToString())
                    .Tag("deviceId", dto.DeviceId.ToString())
                    .Tag("devicePortId", dto.deviceSlaveId.ToString())
                    .Tag("mappingId", dto.MappingId.ToString())
                    .Tag("RegisterAdress", dto.RegisterAddress.ToString())
                    .Tag("SignalName", dto.SignalType.ToString())
                    .Field("value", dto.Value)
                    .Field("unit", dto.Unit)
                    .Timestamp(dto.Timestamp, WritePrecision.Ns);

                var writeApi = _client.GetWriteApiAsync();
                await writeApi.WritePointAsync(point, _bucket, _org);

                //Log.Information("Telemetry written successfully | Asset:{AssetId} | Signal:{SignalTypeId} | Value:{Value}",
                //    dto.AssetId, dto.SignalTypeId, dto.Value);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to write telemetry to InfluxDB | Asset:{AssetId} | Signal:{SignalTypeId}",
                    dto.AssetId, dto.SignalTypeId);
                throw;
            }
        }

        // 🔥 NEW: Flexible method with all time range options
        public async Task<TelemetryResponseDto> GetTelemetrySeriesAsync(TelemetryRequestDto request)
        {
            try
            {
                // Fetch the mapping for the asset + signalType
                var mapping = await _dbContext.MappingTable
                    .FirstOrDefaultAsync(m => m.AssetId == request.AssetId && m.SignalTypeId == request.SignalTypeId);

                if (mapping == null)
                    throw new Exception($"Mapping not found for AssetId:{request.AssetId} and SignalTypeId:{request.SignalTypeId}");

                // Get start and end time
                var (startTime, endTime) = GetTimeRange(request);

                // Build Flux query
                string flux = BuildFluxQuery(mapping.MappingId, startTime, endTime);

                Log.Information("Executing Flux Query | MappingId:{MappingId} | Start:{Start} | End:{End}",
                    mapping.MappingId, startTime, endTime);

                // Query InfluxDB
                var tables = await _queryApi.QueryAsync(flux, _org);
                var values = new List<TelemetryPointDto>();
                int tableCounter = 0;

                foreach (var table in tables)
                {
                    tableCounter++;
                    Log.Information("Processing table #{TableNum} with {RecordCount} records", tableCounter, table.Records.Count);

                    foreach (var record in table.Records)
                    {
                        if (record.GetTime().HasValue && record.GetValue() != null)
                        {
                            values.Add(new TelemetryPointDto
                            {
                                Time = record.GetTime().Value.ToDateTimeUtc().ToLocalTime(),
                                Value = Convert.ToDouble(record.GetValue())
                            });
                        }
                    }
                }

                Log.Information("Total telemetry points fetched: {Count}", values.Count);

                return new TelemetryResponseDto
                {
                    AssetId = mapping.AssetId,
                    DeviceId = mapping.DeviceId,
                    SignalTypeId = mapping.SignalTypeId,
                    SignalName = mapping.SignalName,
                    Unit = mapping.SignalUnit,
                    StartTime = startTime,
                    EndTime = endTime,
                    TimeRange = request.TimeRange.ToString(),
                    Values = values
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to fetch telemetry | AssetId:{AssetId} | SignalTypeId:{SignalTypeId}",
                    request.AssetId, request.SignalTypeId);
                throw new Exception($"Failed to retrieve telemetry: {ex.Message}");
            }
        }



        public async Task<TelemetryResponseDto> GetTelemetrySeriesAsync(Guid assetId, Guid signalTypeId, string startTime)
        {
            DateTime startDateTime;
            DateTime? endDateTime = null;

            // Parse startTime
            if (!DateTime.TryParse(startTime, out startDateTime))
            {
                // Try relative time format
                if (startTime.StartsWith("-"))
                {
                    startDateTime = ParseRelativeTime(startTime);
                }
                else
                {
                    throw new Exception("Invalid startTime format. Use ISO 8601 (2025-12-03T14:30:00Z) or relative (-1h, -24h, -7d)");
                }
            }

            var request = new TelemetryRequestDto
            {
                AssetId = assetId,
                SignalTypeId = signalTypeId,
                TimeRange = TimeRange.Custom,
                StartDate = startDateTime,
                EndDate = endDateTime
            };

            return await GetTelemetrySeriesAsync(request);
        }

        //Helper to calculate the time
        private (DateTime startTime, DateTime endTime) GetTimeRange(TelemetryRequestDto request)
        {
            var now = DateTime.UtcNow;
            DateTime startTime;
            DateTime endTime = now;

            switch (request.TimeRange)
            {
                case TimeRange.LastHour:
                    startTime = now.AddHours(-1);
                    break;

                case TimeRange.Last6Hours:
                    startTime = now.AddHours(-6);
                    break;

                case TimeRange.Last24Hours:
                    startTime = now.AddHours(-24);
                    break;

                case TimeRange.Last7Days:
                    startTime = now.AddDays(-7);
                    break;

                case TimeRange.Last30Days:
                    startTime = now.AddDays(-30);
                    break;

                case TimeRange.Custom:
                    if (!request.StartDate.HasValue)
                        throw new Exception("StartDate is required for Custom time range");

                    startTime = request.StartDate.Value.ToUniversalTime();
                    endTime = request.EndDate?.ToUniversalTime() ?? now;

                    // Validate date range
                    if (startTime >= endTime)
                        throw new Exception("StartDate must be before EndDate");

                    break;

                default:
                    throw new Exception($"Unsupported time range: {request.TimeRange}");
            }

            return (startTime, endTime);
        }

        // helper Build Flux query
        private string BuildFluxQuery(Guid mappingId, DateTime startTime, DateTime endTime)
        {
            var fluxStartTime = startTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var fluxEndTime = endTime.ToString("yyyy-MM-ddTHH:mm:ssZ");


            var window = GetAggregationWindow(startTime, endTime);

            return $@"
                    from(bucket: ""{_bucket}"")
                    |> range(start: {fluxStartTime}, stop: {fluxEndTime})
                    |> filter(fn: (r) => r._field == ""value"")
                    |> filter(fn: (r) => r.mappingId == ""{mappingId}"")
                    |> aggregateWindow(every: {window}, fn: mean, createEmpty: false)
                    |> keep(columns: [""_time"", ""_value""])
                    |> sort(columns: [""_time""], desc: false)";

        }

        // Helper:parse the time string 
        private DateTime ParseRelativeTime(string relativeTime)
        {
            var now = DateTime.UtcNow;

            if (relativeTime == "-1h") return now.AddHours(-1);
            if (relativeTime == "-6h") return now.AddHours(-6);
            if (relativeTime == "-24h") return now.AddHours(-24);
            if (relativeTime == "-7d") return now.AddDays(-7);
            if (relativeTime == "-30d") return now.AddDays(-30);

            // Generic parser for formats like "-5h", "-10d", etc.
            var match = System.Text.RegularExpressions.Regex.Match(relativeTime, @"^-(\d+)([hd])$");
            if (match.Success)
            {
                var value = int.Parse(match.Groups[1].Value);
                var unit = match.Groups[2].Value;

                return unit switch
                {
                    "h" => now.AddHours(-value),
                    "d" => now.AddDays(-value),
                    _ => throw new Exception($"Unsupported time unit: {unit}")
                };
            }

            throw new Exception($"Invalid relative time format: {relativeTime}");
        }


        private string GetAggregationWindow(DateTime start, DateTime end)
        {
            var duration = end - start;

            Log.Information($"the duration is {duration}");



            if (duration <= TimeSpan.FromHours(6))
                return "5s";
            if (duration <= TimeSpan.FromDays(1))
                return "1m";       // Today / last 24h → raw 1s data
            else if (duration <= TimeSpan.FromDays(7))
                return "5m";       // Last 7 days → 1 min aggregation
            else if (duration <= TimeSpan.FromDays(30))
                return "10m";       // Last 1 month → 5 min aggregation
            else if (duration <= TimeSpan.FromDays(90))
                return "30m";      // Last 3 months → 15 min aggregation
            else if (duration <= TimeSpan.FromDays(180))
                return "1h";      // Last 6 months → 30 min aggregation
            else if (duration <= TimeSpan.FromDays(365))
                return "2h";       // Last 1 year → 1 hour aggregation
            else
                return "5h";       // >1 year → 1 hour
        }

    }
}