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

            bool deviceexsit = await _db.MappingTable.AnyAsync(a => a.AssetId == dto.AssetId);
            if (deviceexsit)
                throw new Exception("Asset Already Connected To device");
          

            // Step 2: create mapping for each signal
            try
            {
                List<AssetSignalDeviceMapping> mappings = new();

                foreach (var s in assetSignals)
                {//ye check karta hai ki ek slave ke reguster pe sif ek hi device connect hoga 
                    bool exists = await _db.MappingTable.AnyAsync(m =>
                        m.DeviceId == dto.DeviceId &&
                        m.DevicePortId == dto.DevicePortId &&
                        m.RegisterAdress == s.DefaultRegisterAdress);

                    if (exists)
                        throw new Exception($"Register {s.DefaultRegisterAdress} already exists for this slave.");

                    mappings.Add(new AssetSignalDeviceMapping
                    {
                        AssetId = dto.AssetId,
                        SignalTypeId = s.SignalTypeID,
                        DeviceId = dto.DeviceId,
                        DevicePortId = dto.DevicePortId,
                        RegisterAdress = s.DefaultRegisterAdress,
                        SignalName = s.SignalName,
                        SignalUnit = s.SignalUnit,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                _db.MappingTable.AddRange(mappings);
                await _db.SaveChangesAsync();
       
                return mappings;
            }
            catch
            {
              
                throw;
            }

        }


        public async Task<List<AssetSignalDeviceMapping>> GetMappings()
        {
            return await _db.MappingTable.ToListAsync();
        }
    }
}
