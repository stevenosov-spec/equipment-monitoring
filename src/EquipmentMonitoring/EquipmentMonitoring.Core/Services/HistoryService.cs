using EquipmentMonitoring.Core.Data;
using EquipmentMonitoring.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EquipmentMonitoring.Core.Services
{
    /// <summary>Заглушка истории параметров – генерирует случайные точки для демонстрации трендов</summary>
    public class HistoryService : IHistoryService
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;
        public HistoryService(IDbContextFactory<AppDbContext> contextFactory) => _contextFactory = contextFactory;

        public async Task<List<ParameterHistoryPoint>> GetHistoryAsync(int parameterId, DateTime from, DateTime to)
        {
            await using var db = await _contextFactory.CreateDbContextAsync();
            var param = await db.Parameters.FindAsync(parameterId);
            if (param == null) return new List<ParameterHistoryPoint>();

            // Демо-генерация: берём текущее значение и добавляем небольшой случайный разброс
            var rnd = new Random();
            var points = new List<ParameterHistoryPoint>();
            for (var dt = from; dt <= to; dt = dt.AddMinutes(10))
            {
                points.Add(new ParameterHistoryPoint
                {
                    Timestamp = dt,
                    Value = param.Value + (rnd.NextDouble() - 0.3) * 2
                });
            }
            return points;
        }
    }
}