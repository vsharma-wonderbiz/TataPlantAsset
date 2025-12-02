using System;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Interface;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using Infrastructure.Configuration;
using Infrastructure.DBs;
using Microsoft.Extensions.Options;
using Serilog;


namespace Infrastructure.Service
{
    public class InfluxTelemetryService : IInfluxTelementryService
    {
        private readonly InfluxDBClient _client;
        private readonly string _bucket;
        private readonly string _org;

        public InfluxTelemetryService(
            IInfluxDbConnectionService client,
            IOptions<InfluxDbOptions> options)
        {
            _client = client.GetClient();
            var config = options.Value;
            _bucket = config.InfluxBucket;
            _org = config.InfluxOrg;
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
                    .Tag("RegisterAdress",dto.RegisterAddress.ToString())
                    .Tag("SignalName",dto.SignalType.ToString())
                    .Field("value", dto.Value)
                    .Field("unit", dto.Unit)
                    .Timestamp(dto.Timestamp, WritePrecision.Ns);

               
                 var writeApi = _client.GetWriteApiAsync();

                // Write point asynchronously
                await writeApi.WritePointAsync(point, _bucket, _org);

                Log.Information("Telemetry written successfully | Asset:{AssetId} | Signal:{SignalTypeId} | Value:{Value}",
                    dto.AssetId, dto.SignalTypeId, dto.Value);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to write telemetry to InfluxDB | Asset:{AssetId} | Signal:{SignalTypeId}",
                    dto.AssetId, dto.SignalTypeId);
            }
        }
    }
}
