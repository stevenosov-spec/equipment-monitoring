using System;

namespace EquipmentMonitoring.Core.Services.Interfaces
{
    /// <summary>Источник данных (реальный или симулированный)</summary>
    public interface ITagReader
    {
        event EventHandler<TagValueChangedEventArgs> TagValueChanged;
        void Start();
        void Stop();
    }

    /// <summary>Аргументы события при получении нового значения тега</summary>
    public class TagValueChangedEventArgs : EventArgs
    {
        public string TagAddress { get; set; }
        public double Value { get; set; }
        public DateTime Timestamp { get; set; }
    }
}