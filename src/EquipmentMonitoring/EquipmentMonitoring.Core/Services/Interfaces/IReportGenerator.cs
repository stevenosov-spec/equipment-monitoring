using System;

namespace EquipmentMonitoring.Core.Services.Interfaces
{
    /// <summary>Генератор отчётов в формате Excel</summary>
    public interface IReportGenerator
    {
        byte[] GenerateFaultReport(DateTime from, DateTime to);
    }
}