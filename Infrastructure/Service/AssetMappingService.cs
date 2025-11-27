using Application.Interface;
using System;
using MappingService.Domain.Entities;
using MappingService.DTOs;
using Infrastructure.DBs;
using Microsoft.EntityFrameworkCore;
using Application.DTOs;
using Domain.Entities;

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
            
            //saare signal extract karo 
            var assetSignals = await _db.AssetConfigurations
                .Where(x => x.AssetId == dto.AssetId)
                .Select(x => x.SignalType)
                .ToListAsync();

            if (assetSignals.Count == 0)
                throw new Exception("No signals found for this asset.");

            bool deviceexsit = await _db.MappingTable.AnyAsync(a => a.AssetId == dto.AssetId);
            if (deviceexsit)
                throw new Exception("Asset Already Connected To device");
          

            //har ek isgnal ke liye mapping hoga 
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

        public async Task UnassignDevice(Guid assetId)
        {
            try
            {
                var assetExists = await _db.Assets.AnyAsync(a => a.AssetId == assetId);
                if (!assetExists)
                    throw new Exception("Asset Not Found");

                var mappings = await _db.MappingTable
                                       .Where(m => m.AssetId == assetId)
                                       .ToListAsync();

                if (mappings.Count == 0)
                    throw new Exception("No device mapped to this asset");

                _db.MappingTable.RemoveRange(mappings);
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error while unassigning device: {ex.Message}", ex);
            }
        }

        public async Task<List<AssetSignalDeviceMapping>> GetSignalsOnAnAsset(Guid assetId)
        {
            try
            {
                var assetExists = await _db.Assets.AnyAsync(a => a.AssetId == assetId);
                if (!assetExists)
                    return new List<AssetSignalDeviceMapping>();

                var mappings = await _db.MappingTable
                    .Where(m => m.AssetId == assetId)
                    .ToListAsync();

                if (!mappings.Any())
                    return new List<AssetSignalDeviceMapping>();  

                return mappings;
            }
            catch (Exception ex)
            {
                throw new Exception("Error while fetching mapped signals", ex);
            }
        }

    }
}
