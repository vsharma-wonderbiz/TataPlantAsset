using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class InfluxDbDto
    {
        public Guid MappingId { get; set; }

        // Asset Service
        [Required]
        public Guid AssetId { get; set; }

        // Device Service
        [Required]
        public Guid DeviceId { get; set; }

        [Required]
        public Guid DeviceSlaveId { get; set; }

        [Required]
        public string SignalUnit { get; set; }

        public string SignalName { get; set; }

        public int RegisterAdress { get; set; }
    }
}
