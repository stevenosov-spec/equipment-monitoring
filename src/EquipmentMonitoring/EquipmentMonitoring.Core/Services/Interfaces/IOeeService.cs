// Core/Services/Interfaces/IOeeService.cs
using System;
using System.Threading.Tasks;

namespace EquipmentMonitoring.Core.Services.Interfaces
{
    public interface IOeeService
    {
        /// <summary>
        /// Рассчитывает OEE для указанного оборудования за заданный период.
        /// </summary>
        /// <param name="equipmentId">ID оборудования</param>
        /// <param name="from">Начало периода</param>
        /// <param name="to">Конец периода</param>
        Task<OeeResult> CalculateOeeAsync(int equipmentId, DateTime from, DateTime to);
    }

    public class OeeResult
    {
        public int EquipmentId { get; set; }
        public string EquipmentName { get; set; }
        public double Availability { get; set; }
        public double Performance { get; set; }
        public double Quality { get; set; }
        public double Oee => Availability * Performance * Quality;
    }
}