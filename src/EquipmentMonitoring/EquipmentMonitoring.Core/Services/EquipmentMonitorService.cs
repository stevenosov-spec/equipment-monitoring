using EquipmentMonitoring.Core.Data;
using EquipmentMonitoring.Core.Enums;
using EquipmentMonitoring.Core.Models;
using EquipmentMonitoring.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace EquipmentMonitoring.Core.Services
{
    /// <summary>
    /// Центральный сервис мониторинга оборудования.
    /// Получает данные от <see cref="ITagReader"/>, обновляет параметры в БД,
    /// обнаруживает выход за границы допуска и создаёт отказы.
    /// </summary>
    public class EquipmentMonitorService : IEquipmentMonitor, IDisposable
    {
        private readonly ITagReader _tagReader;
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        /// <inheritdoc/>
        public event Action<Fault>? OnFaultDetected;

        /// <inheritdoc/>
        public event Action<int, EquipmentState>? OnStateChanged;

        /// <summary>
        /// Инициализирует сервис мониторинга.
        /// </summary>
        /// <param name="tagReader">Источник данных (реальный или симулированный).</param>
        /// <param name="contextFactory">Фабрика контекста БД для потокобезопасного доступа.</param>
        public EquipmentMonitorService(ITagReader tagReader, IDbContextFactory<AppDbContext> contextFactory)
        {
            _tagReader = tagReader;
            _contextFactory = contextFactory;
            _tagReader.TagValueChanged += OnTagValueChanged;
        }

        /// <inheritdoc/>
        public void Start() => _tagReader.Start();

        /// <inheritdoc/>
        public void Stop() => _tagReader.Stop();

        /// <summary>
        /// Обрабатывает новое значение тега: обновляет соответствующий параметр,
        /// проверяет выход за границы, при необходимости создаёт отказ и обновляет состояние оборудования.
        /// </summary>
        /// <param name="sender">Источник события (может быть null).</param>
        /// <param name="e">Аргументы события с адресом тега, значением и временной меткой.</param>
        private void OnTagValueChanged(object? sender, TagValueChangedEventArgs e)
        {
            using var db = _contextFactory.CreateDbContext();
            var param = db.Parameters.Include(p => p.Equipment).FirstOrDefault(p => p.TagAddress == e.TagAddress);
            if (param == null) return;

            param.Value = e.Value;
            param.Timestamp = e.Timestamp;
            db.SaveChanges();

            if (e.Value < param.MinAllowed || e.Value > param.MaxAllowed)
            {
                var fault = new Fault
                {
                    EquipmentId = param.EquipmentId,
                    StartTime = e.Timestamp,
                    Description = $"Параметр '{param.Name}' вне допуска: {e.Value} {param.Unit}",
                    Priority = DeterminePriority(param, e.Value),
                    Status = FaultStatus.Active
                };
                db.Faults.Add(fault);
                db.SaveChanges();
                OnFaultDetected?.Invoke(fault);
            }

            var hasActiveFault = db.Faults.Any(f => f.EquipmentId == param.EquipmentId && f.Status == FaultStatus.Active);
            var newState = hasActiveFault ? EquipmentState.Alarm : EquipmentState.Normal;
            if (param.Equipment.CurrentState != newState)
            {
                param.Equipment.CurrentState = newState;
                db.SaveChanges();
                OnStateChanged?.Invoke(param.EquipmentId, newState);
            }
        }

        /// <summary>
        /// Вычисляет приоритет отказа на основе относительного отклонения значения от допустимых границ.
        /// </summary>
        /// <param name="param">Параметр, для которого проверяется отклонение.</param>
        /// <param name="value">Текущее значение.</param>
        /// <returns>Приоритет отказа.</returns>
        private static FaultPriority DeterminePriority(Parameter param, double value)
        {
            double range = param.MaxAllowed - param.MinAllowed;
            if (range == 0) return FaultPriority.High;
            double deviation = Math.Max((param.MinAllowed - value) / range, (value - param.MaxAllowed) / range);
            if (deviation > 0.3) return FaultPriority.Critical;
            if (deviation > 0.15) return FaultPriority.High;
            return FaultPriority.Medium;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _tagReader.TagValueChanged -= OnTagValueChanged;
            _tagReader.Stop();
            GC.SuppressFinalize(this);
        }
    }
}