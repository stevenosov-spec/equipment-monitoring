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

        public async Task<OeeResult> CalculateOeeAsync(int equipmentId, DateTime from, DateTime to)
        {
            if (from > to)
                (from, to) = (to, from);

            await using var db = await _contextFactory.CreateDbContextAsync();
            var equipment = await db.Equipments.FindAsync(equipmentId);
            if (equipment == null) return null;

            // === ДОСТУПНОСТЬ (Availability) ===
            double totalMinutes = (to - from).TotalMinutes;
            double downMinutes = 0;                          // ← ОБНУЛЯЕМ ПЕРЕД ЦИКЛОМ

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
                    downMinutes += (faultEnd - faultStart).TotalMinutes;   // ← СУММИРУЕМ
            }

            double availability = totalMinutes > 0
                ? (totalMinutes - downMinutes) / totalMinutes
                : 1.0;

            // === ПРОИЗВОДИТЕЛЬНОСТЬ (Performance) ===
            double performance = 1.0;
            var perfParam = await db.Parameters
                .FirstOrDefaultAsync(p => p.EquipmentId == equipmentId && p.NominalValue > 0);

            if (perfParam != null && perfParam.NominalValue > 0)
            {
                double currentValue = perfParam.Value;
                performance = currentValue / perfParam.NominalValue;
                performance = Math.Max(0, Math.Min(1.5, performance));
            }

            // === КАЧЕСТВО (Quality) ===
            double quality = 1.0;

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