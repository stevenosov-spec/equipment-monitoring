using System.Collections.Generic;
using EquipmentMonitoring.Core.Enums;

namespace EquipmentMonitoring.Core.Models
{
    /// <summary>Единица технологического оборудования</summary>
    public class Equipment
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }          // Насос, Печь, Компрессор...
        public string Installation { get; set; }  // Установка (АВТ, УПН...)
        public EquipmentState CurrentState { get; set; } = EquipmentState.NoData;

        // Навигационные свойства для EF Core
        public List<Parameter> Parameters { get; set; } = new();
        public List<Fault> Faults { get; set; } = new();
    }
}