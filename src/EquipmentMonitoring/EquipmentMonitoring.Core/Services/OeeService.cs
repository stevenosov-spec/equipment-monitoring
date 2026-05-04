// Core/Services/OeeService.cs
using EquipmentMonitoring.Core.Data;
using EquipmentMonitoring.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace EquipmentMonitoring.Core.Services
{
    public class OeeService : IOeeService
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public OeeService(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        /// <summary>
        /// Рассчитывает OEE для указанного оборудования за заданный интервал [from, to].
        /// </summary>
        public async Task<OeeResult> CalculateOeeAsync(int equipmentId, DateTime from, DateTime to)
        {
            if (from > to)
                (from, to) = (to, from);   // на всякий случай меняем местами

            await using var db = await _contextFactory.CreateDbContextAsync();
            var equipment = await db.Equipments.FindAsync(equipmentId);
            if (equipment == null) return null;

            // --- Доступность (Availability) ---
            double totalMinutes = (to - from).TotalMinutes;
            double downMinutes = 0;

            var faults = await db.Faults
                .Where(f => f.EquipmentId == equipmentId)
                .Where(f => f.StartTime < to && (f.EndTime == null || f.EndTime > from))
                .ToListAsync();

            foreach (var fault in faults)
            {
                var faultStart = fault.StartTime < from ? from : fault.StartTime;
                var faultEnd = fault.EndTime ?? to;
                if (faultEnd > to) faultEnd = to;
                if (faultEnd > faultStart)
                    downMinutes += (faultEnd - faultStart).TotalMinutes;
            }

            double availability = totalMinutes > 0 ? (totalMinutes - downMinutes) / totalMinutes : 1.0;

            // --- Производительность (Performance) ---
            double performance = 1.0;
            // Берём первый параметр с NominalValue > 0 для этого оборудования
            var perfParam = await db.Parameters
                .FirstOrDefaultAsync(p => p.EquipmentId == equipmentId && p.NominalValue > 0);
            if (perfParam != null && perfParam.NominalValue > 0)
            {
                // Используем текущее значение параметра (или можно среднее за период)
                double currentValue = perfParam.Value;
                performance = currentValue / perfParam.NominalValue;
                performance = Math.Max(0, Math.Min(1.5, performance));
            }

            // --- Качество (Quality) ---
            double quality = 1.0; // заглушка

            return new OeeResult
            {
                EquipmentId = equipment.Id,
                EquipmentName = equipment.Name,
                Availability = Math.Round(availability, 3),
                Performance = Math.Round(performance, 3),
                Quality = quality
            };
        }
    }
}