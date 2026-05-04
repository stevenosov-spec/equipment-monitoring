using System;
using System.Collections.Generic;
using EquipmentMonitoring.Core.Services.Interfaces;

namespace EquipmentMonitoring.Integration.Simulation
{
    /// <summary>
    /// Улучшенный симулятор датчиков: генерирует плавно меняющиеся значения,
    /// имитируя реальный технологический процесс (случайное блуждание с возвратом к среднему).
    /// </summary>
    public class SimulatedTagReader : ITagReader
    {
        private System.Timers.Timer _timer;
        private readonly Random _random = new();
        // Конфигурация тегов: ключ — TagAddress, значения — min, max, avg
        private readonly Dictionary<string, (double min, double max, double avg)> _tags;
        // Текущее значение каждого тега (используется для плавного изменения)
        private readonly Dictionary<string, double> _currentValues = new(StringComparer.OrdinalIgnoreCase);

        public event EventHandler<TagValueChangedEventArgs> TagValueChanged;

        public SimulatedTagReader()
        {
            _tags = new Dictionary<string, (double, double, double)>
            {
                // Насос
                { "Flow_Pump101", (0, 100, 50) },
                { "Temp_Pump101", (20, 80, 40) },
                { "Pressure_Pump101", (0, 10, 2.5) },
                { "Vibration_Pump101", (0, 5, 2) },
                // Печь
                { "Temp_Furnace", (800, 1200, 1000) },
                { "Flow_Fuel", (100, 500, 300) },
                { "Pressure_Furnace", (-50, 50, 0) },
                // Компрессор
                { "Temp_Compressor", (60, 110, 85) },
                { "Pressure_CompIn", (1, 8, 4.5) },
                { "Vibration_Comp", (0, 7.1, 3.5) }
            };

            // Инициализируем текущие значения средними (равновесное состояние)
            foreach (var tag in _tags)
            {
                _currentValues[tag.Key] = tag.Value.avg;
            }
        }

        public void Start()
        {
            _timer = new System.Timers.Timer(1000); // обновление каждую секунду
            _timer.Elapsed += OnTimer;
            _timer.AutoReset = true;
            _timer.Start();
        }

        public void Stop()
        {
            _timer?.Stop();
            _timer?.Dispose();
        }

        /// <summary>
        /// Каждую секунду для каждого тега вычисляется новое значение:
        ///   newValue = oldValue + случайное приращение + возврат к среднему
        /// Это создаёт плавные, инерционные изменения.
        /// </summary>
        private void OnTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            foreach (var tag in _tags)
            {
                string key = tag.Key;
                var (min, max, avg) = tag.Value;

                double current = _currentValues[key];

                // Случайная составляющая изменения (нормальное распределение вокруг нуля)
                double sigma = (max - min) * 0.05; // 5% от диапазона — амплитуда случайных колебаний
                double randomStep = (_random.NextDouble() - 0.5) * sigma * 0.5; // [-sigma/4, sigma/4]

                // Возврат к среднему значению (small mean reversion)
                double meanReversion = (avg - current) * 0.005; // 0.5% за шаг

                double newValue = current + randomStep + meanReversion;

                // Ограничиваем значения в пределах min..max (но иногда можно выходить для имитации отклонений)
                newValue = Math.Max(min - (max - min) * 0.05, Math.Min(max + (max - min) * 0.05, newValue));

                _currentValues[key] = newValue;

                // Отправляем событие
                TagValueChanged?.Invoke(this, new TagValueChangedEventArgs
                {
                    TagAddress = key,
                    Value = Math.Round(newValue, 2),
                    Timestamp = DateTime.Now
                });
            }
        }
    }
}