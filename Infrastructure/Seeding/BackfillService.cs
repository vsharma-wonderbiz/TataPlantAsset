using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InfluxDB.Client;
using InfluxDB.Client.Writes;
using Microsoft.Extensions.Options;
using Application.Interface;
using Infrastructure.Configuration;
using InfluxDB.Client.Api.Domain;

namespace Infrastructure.Seeding
{
    public class BackfillService
    {
        private readonly InfluxDBClient _client;
        private readonly string _bucket;
        private readonly string _org;

        public BackfillService(
            IInfluxDbConnectionService client,
            IOptions<InfluxDbOptions> options)
        {
            _client = client.GetClient();
            var config = options.Value;
            _bucket = config.InfluxBucket;
            _org = config.InfluxOrg;
        }

        public async Task GenerateBackfillData()
        {
            Console.WriteLine($"[INFO] Backfill started at {DateTime.Now}");

            try
            {
                var writeApi = _client.GetWriteApi();
                DateTime startLocal = DateTime.Now.AddMonths(-1);
                DateTime endLocal = DateTime.Now;

                Random rand = new Random();
                int counter = 0;
                int batchSize = 5000; // number of points per batch
                var batch = new List<PointData>();

                for (var timestampLocal = startLocal; timestampLocal <= endLocal; timestampLocal = timestampLocal.AddSeconds(5))
                {
                    double flowRate = 9 + rand.NextDouble() * 2;

                    var point = PointData.Measurement("signals")
                        .Tag("assetId", "bb81bad0-495d-459b-0f78-08de36f00907")
                        .Tag("signalTypeId", "f2821c2d-b350-415a-aa89-ef6ad98e4505")
                        .Tag("deviceId", "c337db3e-68cd-44aa-bc05-ee5508b25216")
                        .Tag("devicePortId", "e9bbf4c1-8a04-4afa-a546-036f8771afb3")
                        .Tag("mappingId", "a2bee135-6ae6-456e-a938-9a372dd0bbe7")
                        .Tag("RegisterAdress", "40011")
                        .Tag("SignalName", "FlowRate")
                        .Field("value", flowRate)
                        .Field("unit", "L/min")
                        .Timestamp(timestampLocal, WritePrecision.Ns);

                    batch.Add(point);
                    counter++;

                    // Write batch if it reaches batchSize
                    if (batch.Count >= batchSize)
                    {
                        try
                        {
                            writeApi.WritePoints(batch, _bucket, _org);
                            Console.WriteLine($"[INFO] {counter} points written, last timestamp: {timestampLocal}");
                            batch.Clear();
                        }
                        catch (Exception exBatch)
                        {
                            Console.WriteLine($"[ERROR] Failed to write batch ending at {timestampLocal}: {exBatch.Message}");
                        }
                    }
                }

                // Write any remaining points
                if (batch.Count > 0)
                {
                    writeApi.WritePoints(batch, _bucket, _org);
                    Console.WriteLine($"[INFO] Final batch of {batch.Count} points written, total points: {counter}");
                }

                Console.WriteLine($"[INFO] Backfill completed successfully at {DateTime.Now}, total points written: {counter}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Backfill process failed: {ex.Message}");
            }
        }
    }
}
