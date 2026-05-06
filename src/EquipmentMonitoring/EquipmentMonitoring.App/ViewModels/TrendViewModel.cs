using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentMonitoring.Core.Services.Interfaces;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace EquipmentMonitoring.App.ViewModels
{
    public partial class TrendViewModel : ObservableObject
    {
        // Доступные масштабы
        public ObservableCollection<string> Scales { get; } = new()
        {
            "Последний час",
            "6 часов",
            "Сутки",
            "Неделя",
            "Месяц",
            "Произвольный"
        };

        private readonly IHistoryService _historyService;
        private readonly int _parameterId;
        private readonly string _parameterName;
        private readonly double _minAllowed;
        private readonly double _maxAllowed;

        [ObservableProperty]
        private PlotModel _plotModel;

        [ObservableProperty]
        private DateTime _startDate = DateTime.Now.AddDays(-1);

        [ObservableProperty]
        private DateTime _endDate = DateTime.Now;

        [ObservableProperty]
        private string _selectedScale = "Сутки"; // по умолчанию

        public IRelayCommand RefreshCommand { get; }

        public TrendViewModel(IHistoryService historyService,
                              int parameterId,
                              string parameterName,
                              double minAllowed,
                              double maxAllowed)
        {
            _historyService = historyService;
            _parameterId = parameterId;
            _parameterName = parameterName;
            _minAllowed = minAllowed;
            _maxAllowed = maxAllowed;
            RefreshCommand = new RelayCommand(async () => await LoadDataAsync());
            _ = LoadDataAsync();
        }

        // При изменении масштаба пересчитываем даты и загружаем данные
        partial void OnSelectedScaleChanged(string value)
        {
            DateTime now = DateTime.Now;
            EndDate = now;

            StartDate = value switch
            {
                "Последний час" => now.AddHours(-1),
                "6 часов" => now.AddHours(-6),
                "Сутки" => now.AddDays(-1),
                "Неделя" => now.AddDays(-7),
                "Месяц" => now.AddMonths(-1),
                _ => StartDate // для "Произвольный" оставляем текущие значения
            };

            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            var points = await _historyService.GetHistoryAsync(_parameterId, StartDate, EndDate);
            var model = new PlotModel { Title = $"Тренд: {_parameterName}" };
            model.Axes.Add(new DateTimeAxis { Position = AxisPosition.Bottom, Title = "Время" });
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Значение" });

            var series = new LineSeries { Title = _parameterName };
            foreach (var p in points.OrderBy(p => p.Timestamp))
                series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(p.Timestamp), p.Value));
            model.Series.Add(series);

            // Границы допуска
            if (_maxAllowed > _minAllowed)
            {
                model.Annotations.Add(new LineAnnotation
                {
                    Type = LineAnnotationType.Horizontal,
                    Y = _maxAllowed,
                    Color = OxyColors.Red,
                    LineStyle = LineStyle.Dash,
                    Text = $"Верхняя граница ({_maxAllowed})",
                    TextColor = OxyColors.Red,
                    FontSize = 10,
                    TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Left,
                    TextVerticalAlignment = OxyPlot.VerticalAlignment.Top
                });

                model.Annotations.Add(new LineAnnotation
                {
                    Type = LineAnnotationType.Horizontal,
                    Y = _minAllowed,
                    Color = OxyColors.Red,
                    LineStyle = LineStyle.Dash,
                    Text = $"Нижняя граница ({_minAllowed})",
                    TextColor = OxyColors.Red,
                    FontSize = 10,
                    TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Left,
                    TextVerticalAlignment = OxyPlot.VerticalAlignment.Bottom
                });
            }

            PlotModel = model;
        }
    }
}