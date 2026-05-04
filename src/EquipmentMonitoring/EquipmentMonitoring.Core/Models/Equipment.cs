using System.Collections.Generic;
using EquipmentMonitoring.Core.Enums;

namespace EquipmentMonitoring.Core.Models
{
    /// <summary>Единица технологического оборудования</summary>
    public class Equipment
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;        // инициализация
        public string Type { get; set; } = string.Empty;
        public string Installation { get; set; } = string.Empty;
        public EquipmentState CurrentState { get; set; } = EquipmentState.NoData;

        public List<Parameter> Parameters { get; set; } = new();
        public List<Fault> Faults { get; set; } = new();
    }
}