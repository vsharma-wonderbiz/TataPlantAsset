using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.ReportDTos
{
    public class ReportQueueItem
    {
        public Guid AssetId { get; set; }
        public List<Guid> SignalIds { get; set; }
        public List<string> MappingIds { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string ReportFormat { get; set; }
        public long TotalRows { get; set; }
        public DateTime RequestedAt { get; set; }
    }
}
