using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class CreateMappingDto
    {
        public Guid AssetId { get; set; }
        public Guid DeviceId { get; set; }
        public Guid DevicePortId { get; set; }
    }

}
