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
                    new SignalTypes { SignalTypeID = Guid.NewGuid(), SignalName = "Temperature", SignalUnit = "°C" ,DefaultRegisterAdress=40001},
                    new SignalTypes { SignalTypeID = Guid.NewGuid(), SignalName = "Pressure", SignalUnit = "kPa" ,DefaultRegisterAdress=40003},
                    new SignalTypes { SignalTypeID = Guid.NewGuid(), SignalName = "Voltage", SignalUnit = "V" ,DefaultRegisterAdress=40005},
                    new SignalTypes { SignalTypeID = Guid.NewGuid(), SignalName = "Current", SignalUnit = "A" ,DefaultRegisterAdress=40007},
                    new SignalTypes { SignalTypeID = Guid.NewGuid(), SignalName = "Flow", SignalUnit = "L/min" ,DefaultRegisterAdress=40009},
                    new SignalTypes { SignalTypeID = Guid.NewGuid(), SignalName = "Vibration", SignalUnit = "mm/s" ,DefaultRegisterAdress=40011},
                    new SignalTypes { SignalTypeID = Guid.NewGuid(), SignalName = "RPM", SignalUnit = "rpm",DefaultRegisterAdress=40013 }
                };

                await dbContext.SignalTypes.AddRangeAsync(SignalTypess);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
