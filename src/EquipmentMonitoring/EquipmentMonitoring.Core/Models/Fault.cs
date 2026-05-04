using System;
using EquipmentMonitoring.Core.Enums;

namespace EquipmentMonitoring.Core.Models
{
    public class Fault
    {
        public int Id { get; set; }
        public int EquipmentId { get; set; }
        public Equipment? Equipment { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Description { get; set; } = string.Empty;
        public FaultPriority Priority { get; set; }
        public FaultStatus Status { get; set; } = FaultStatus.Active;
    }
}