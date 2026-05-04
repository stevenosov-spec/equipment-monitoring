using CommunityToolkit.Mvvm.ComponentModel;
using EquipmentMonitoring.Core.Enums;

namespace EquipmentMonitoring.App.ViewModels
{
    /// <summary>Обёртка для модели Equipment, добавляет поддержку привязки</summary>
    public partial class EquipmentViewModel : ObservableObject
    {
        public int Id { get; }
        public string Name { get; }
        public string Installation { get; }

        [ObservableProperty]   // автоматически создаст свойство State
        private EquipmentState _state;

        public EquipmentViewModel(Core.Models.Equipment equipment)
        {
            Id = equipment.Id;
            Name = equipment.Name;
            Installation = equipment.Installation;
            _state = equipment.CurrentState;
        }
    }
}