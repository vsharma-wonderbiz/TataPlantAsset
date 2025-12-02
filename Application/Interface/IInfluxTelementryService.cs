using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs;
using Microsoft.Identity.Client.TelemetryCore.TelemetryClient;
using Application.DTOs;

namespace Application.Interface
{
    public interface IInfluxTelementryService
    {
        Task WriteTelemetryAsync(InfluxTelementryDto Dto);
        //Task<TelemetryData> ReadLatestAsync(string assetId, string signalId);
    }
}
