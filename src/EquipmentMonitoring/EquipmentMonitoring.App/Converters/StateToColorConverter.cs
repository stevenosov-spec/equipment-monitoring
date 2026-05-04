using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using EquipmentMonitoring.Core.Enums;

namespace EquipmentMonitoring.App.Converters
{
    /// <summary>Преобразует EquipmentState в цвет кисти</summary>
    public class StateToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is EquipmentState state)
            {
                return state switch
                {
                    EquipmentState.Normal => new SolidColorBrush(Colors.Green),
                    EquipmentState.Warning => new SolidColorBrush(Colors.Orange),
                    EquipmentState.Alarm => new SolidColorBrush(Colors.Red),
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}