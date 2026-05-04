using CommunityToolkit.Mvvm.ComponentModel;
using System;
using EquipmentMonitoring.Core.Enums;

namespace EquipmentMonitoring.App.ViewModels
{
    public partial class FaultViewModel : ObservableObject
    {
        public int Id { get; }
        public int EquipmentId { get; }   // ✅ ID оборудования, которому принадлежит отказ
        public DateTime StartTime { get; }
        public string Description { get; }

        [ObservableProperty]
        private FaultStatus _status;

        public FaultViewModel(Core.Models.Fault fault)
        {
            Id = fault.Id;
            EquipmentId = fault.EquipmentId;   // сохраняем ID оборудования
            StartTime = fault.StartTime;
            Description = fault.Description;
            _status = fault.Status;
        }
    }
}