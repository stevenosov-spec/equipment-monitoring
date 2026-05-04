using EquipmentMonitoring.Core.Enums;
using EquipmentMonitoring.Core.Models;
using System;

namespace EquipmentMonitoring.Core.Services.Interfaces
{
    /// <summary>Сервис мониторинга оборудования</summary>
    public interface IEquipmentMonitor
    {
        event Action<Fault> OnFaultDetected;            // оповещение о новом отказе
        event Action<int, EquipmentState> OnStateChanged; // изменение состояния оборудования
        void Start();
        void Stop();
    }
}