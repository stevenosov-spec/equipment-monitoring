using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace EquipmentMonitoring.App.ViewModels
{
    public partial class ParameterViewModel : ObservableObject
    {
        public int Id { get; }
        public string Name { get; }
        public string Unit { get; }
        public double MinAllowed { get; }   // нижняя граница допуска
        public double MaxAllowed { get; }   // верхняя граница допуска

        [ObservableProperty]
        private double _value;

        [ObservableProperty]
        private DateTime _timestamp;

        public ParameterViewModel(Core.Models.Parameter parameter)
        {
            Id = parameter.Id;
            Name = parameter.Name;
            Unit = parameter.Unit;
            MinAllowed = parameter.MinAllowed;
            MaxAllowed = parameter.MaxAllowed;
            _value = parameter.Value;
            _timestamp = parameter.Timestamp;
        }
    }
}