using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EquipmentMonitoring.Core.Services.Interfaces
{
    /// <summary>Сервис для получения исторических данных параметра (для трендов)</summary>
    public interface IHistoryService
    {
        Task<List<ParameterHistoryPoint>> GetHistoryAsync(int parameterId, DateTime from, DateTime to);
    }

    /// <summary>Одна точка исторических данных</summary>
    public class ParameterHistoryPoint
    {
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
    }
}