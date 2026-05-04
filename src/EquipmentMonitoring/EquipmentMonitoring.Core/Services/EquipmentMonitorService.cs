using EquipmentMonitoring.Core.Data;
using EquipmentMonitoring.Core.Enums;
using EquipmentMonitoring.Core.Models;
using EquipmentMonitoring.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace EquipmentMonitoring.Core.Services
{
    /// <summary>Основной сервис мониторинга: обрабатывает поступающие данные и выявляет отказы</summary>
    public class EquipmentMonitorService : IEquipmentMonitor, IDisposable
    {
        private readonly ITagReader _tagReader;
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public event Action<Fault> OnFaultDetected;
        public event Action<int, EquipmentState> OnStateChanged;

        public EquipmentMonitorService(ITagReader tagReader, IDbContextFactory<AppDbContext> contextFactory)
        {
            _tagReader = tagReader;
            _contextFactory = contextFactory;
            // Подписываемся на новые данные от источника
            _tagReader.TagValueChanged += OnTagValueChanged;
        }

        public void Start() => _tagReader.Start();
        public void Stop() => _tagReader.Stop();

        /// <summary>Обработчик нового значения тега: обновляет параметр, проверяет границы, создаёт отказы</summary>
        private void OnTagValueChanged(object sender, TagValueChangedEventArgs e)
        {
            // Потокобезопасное создание контекста
            using var db = _contextFactory.CreateDbContext();
            var param = db.Parameters.Include(p => p.Equipment).FirstOrDefault(p => p.TagAddress == e.TagAddress);
            if (param == null) return;

            // Обновляем текущее значение
            param.Value = e.Value;
            param.Timestamp = e.Timestamp;
            db.SaveChanges();

            // Проверка выхода за допустимые границы
            if (e.Value < param.MinAllowed || e.Value > param.MaxAllowed)
            {
                var fault = new Fault
                {
                    EquipmentId = param.EquipmentId,
                    StartTime = e.Timestamp,
                    Description = $"Параметр '{param.Name}' вне допуска: {e.Value} {param.Unit}",
                    Priority = DeterminePriority(param, e.Value),   // расчёт важности
                    Status = FaultStatus.Active
                };
                db.Faults.Add(fault);
                db.SaveChanges();
                OnFaultDetected?.Invoke(fault);  // оповещаем UI
            }

            // Обновляем общее состояние оборудования (если есть активный отказ → Alarm)
            var hasActiveFault = db.Faults.Any(f => f.EquipmentId == param.EquipmentId && f.Status == FaultStatus.Active);
            var newState = hasActiveFault ? EquipmentState.Alarm : EquipmentState.Normal;
            if (param.Equipment.CurrentState != newState)
            {
                param.Equipment.CurrentState = newState;
                db.SaveChanges();
                OnStateChanged?.Invoke(param.EquipmentId, newState);
            }
        }

        /// <summary>Простая эвристика приоритета: чем сильнее отклонение, тем выше приоритет</summary>
        private FaultPriority DeterminePriority(Parameter param, double value)
        {
            double range = param.MaxAllowed - param.MinAllowed;
            if (range == 0) return FaultPriority.High;
            double deviation = Math.Max((param.MinAllowed - value) / range, (value - param.MaxAllowed) / range);
            if (deviation > 0.3) return FaultPriority.Critical;
            if (deviation > 0.15) return FaultPriority.High;
            return FaultPriority.Medium;
        }

        public void Dispose()
        {
            _tagReader.TagValueChanged -= OnTagValueChanged;
            _tagReader.Stop();
        }
    }
}