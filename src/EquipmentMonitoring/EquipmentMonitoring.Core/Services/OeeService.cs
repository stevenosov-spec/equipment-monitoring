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

        public async Task<OeeResult> CalculateOeeAsync(int equipmentId)
        {
            var now = DateTime.Now;
            var periodStart = now.AddHours(-1); // анализируем последний час

            await using var db = await _contextFactory.CreateDbContextAsync();
            var equipment = await db.Equipments.FindAsync(equipmentId);
            if (equipment == null) return null;

            // --- Доступность (Availability) ---
            double totalMinutes = (now - periodStart).TotalMinutes;
            double downMinutes = 0;

            // Учитываем отказы, которые были активны в течение периода
            var faults = await db.Faults
                .Where(f => f.EquipmentId == equipmentId)
                .Where(f => f.StartTime < now && (f.EndTime == null || f.EndTime > periodStart))
                .ToListAsync();

            foreach (var fault in faults)
            {
                var faultStart = fault.StartTime < periodStart ? periodStart : fault.StartTime;
                var faultEnd = fault.EndTime ?? now;
                if (faultEnd > now) faultEnd = now;
                if (faultEnd > faultStart)
                    downMinutes += (faultEnd - faultStart).TotalMinutes;
            }

            double availability = totalMinutes > 0 ? (totalMinutes - downMinutes) / totalMinutes : 1.0;

            // --- Производительность (Performance) ---
            double performance = 1.0;
            var perfParam = await db.Parameters
                .FirstOrDefaultAsync(p => p.EquipmentId == equipmentId && p.NominalValue > 0);
            if (perfParam != null && perfParam.NominalValue > 0)
            {
                performance = perfParam.Value / perfParam.NominalValue;
                performance = Math.Max(0, Math.Min(1.5, performance)); // ограничиваем 0..1.5
            }

            // --- Качество (Quality) ---
            double quality = 1.0; // нет данных о браке

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