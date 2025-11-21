using Application.Interface;
using System;
using MappingService.Domain.Entities;
using MappingService.DTOs;
using Infrastructure.DBs;
using Microsoft.EntityFrameworkCore;
using Application.DTOs;

namespace Infrastructure.Services
{
    public class AssetMappingService : IMappingService
    {
        private readonly DBContext _db;

        public AssetMappingService(DBContext db)
        {
            _db = db;
        }

        public async Task<List<AssetSignalDeviceMapping>> CreateMapping(CreateMappingDto dto)
        {
            // Step 1: get all signals of this asset
            var assetSignals = await _db.AssetConfigurations
                .Where(x => x.AssetId == dto.AssetId)
                .Select(x => x.SignalType)
                .ToListAsync();

            if (assetSignals.Count == 0)
                throw new Exception("No signals found for this asset.");

            // Step 2: create mapping for each signal
            List<AssetSignalDeviceMapping> mappings = new();

            foreach (var s in assetSignals)
            {
                var mapping = new AssetSignalDeviceMapping
                {
                    AssetId = dto.AssetId,
                    SignalTypeId = s.SignalTypeID,
                    DeviceId = dto.DeviceId,
                    DevicePortId = dto.DevicePortId,
                    RegisterAdress = s.DefaultRegisterAdress,
                    SignalName = s.SignalName,
                    SignalUnit=s.SignalUnit,
                    CreatedAt = DateTime.UtcNow
                };

                _db.MappingTable.Add(mapping);
                mappings.Add(mapping);
            }

            await _db.SaveChangesAsync();
            return mappings;
        }


        public async Task<List<AssetSignalDeviceMapping>> GetMappings()
        {
            return await _db.MappingTable.ToListAsync();
        }
    }
}
