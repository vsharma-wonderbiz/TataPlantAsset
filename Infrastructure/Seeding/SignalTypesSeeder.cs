using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities;
using Infrastructure.DBs;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Seeding
{
    public static class SignalTypessSeeder
    {
        public static async Task SeedAsync(DBContext dbContext)
        {
            if (!await dbContext.SignalTypes.AnyAsync())
            {
                var SignalTypess = new List<SignalTypes>
                {
                    new SignalTypes { SignalTypeID = Guid.NewGuid(), SignalName = "Temperature", SignalUnit = "°C" },
                    new SignalTypes { SignalTypeID = Guid.NewGuid(), SignalName = "Pressure", SignalUnit = "kPa" },
                    new SignalTypes { SignalTypeID = Guid.NewGuid(), SignalName = "Voltage", SignalUnit = "V" },
                    new SignalTypes { SignalTypeID = Guid.NewGuid(), SignalName = "Current", SignalUnit = "A" },
                    new SignalTypes { SignalTypeID = Guid.NewGuid(), SignalName = "Flow", SignalUnit = "L/min" },
                    new SignalTypes { SignalTypeID = Guid.NewGuid(), SignalName = "Vibration", SignalUnit = "mm/s" },
                    new SignalTypes { SignalTypeID = Guid.NewGuid(), SignalName = "RPM", SignalUnit = "rpm" }
                };

                await dbContext.SignalTypes.AddRangeAsync(SignalTypess);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
