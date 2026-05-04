using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentMonitoring.Core.Services.Interfaces;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace EquipmentMonitoring.App.ViewModels
{
    public partial class TrendViewModel : ObservableObject
    {
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

        private async Task LoadDataAsync()
        {
            var points = await _historyService.GetHistoryAsync(_parameterId, StartDate, EndDate);
            var model = new PlotModel { Title = $"Тренд: {_parameterName}" };
            model.Axes.Add(new DateTimeAxis { Position = AxisPosition.Bottom, Title = "Время" });
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Значение" });

            // Исторический ряд
            var series = new LineSeries { Title = _parameterName };
            foreach (var p in points.OrderBy(p => p.Timestamp))
                series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(p.Timestamp), p.Value));
            model.Series.Add(series);

            // Добавляем границы допустимых значений
            if (_maxAllowed > _minAllowed) // только если заданы реальные границы
            {
                // Верхняя граница
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

                // Нижняя граница
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