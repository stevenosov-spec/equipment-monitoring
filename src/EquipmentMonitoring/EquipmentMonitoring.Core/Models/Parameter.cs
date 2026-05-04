using System;

namespace EquipmentMonitoring.Core.Models
{
    public class Parameter
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double Value { get; set; }
        public DateTime Timestamp { get; set; }
        public string Unit { get; set; }
        public string TagAddress { get; set; }
        public double MinAllowed { get; set; }
        public double MaxAllowed { get; set; }
        public double NominalValue { get; set; }    

        public int EquipmentId { get; set; }
        public Equipment Equipment { get; set; }
    }
}