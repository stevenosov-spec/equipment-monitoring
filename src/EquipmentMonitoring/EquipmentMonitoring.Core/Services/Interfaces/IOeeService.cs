using System.Threading.Tasks;

namespace EquipmentMonitoring.Core.Services.Interfaces
{
    /// <summary>Сервис расчёта OEE</summary>
    public interface IOeeService
    {
        Task<OeeResult> CalculateOeeAsync(int equipmentId);
    }

    /// <summary>Результат расчёта OEE</summary>
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