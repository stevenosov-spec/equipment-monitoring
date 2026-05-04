using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentMonitoring.Core.Data;
using EquipmentMonitoring.Core.Enums;
using EquipmentMonitoring.Core.Models;
using EquipmentMonitoring.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using EquipmentMonitoring.App.Views;

namespace EquipmentMonitoring.App.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IEquipmentMonitor _monitor;
        private readonly IReportGenerator _reportGenerator;
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly IHistoryService _historyService;
        private readonly IOeeService _oeeService;               // ✅ сервис OEE
        private System.Timers.Timer _refreshTimer;

        public ObservableCollection<EquipmentViewModel> Equipments { get; } = new();
        public ObservableCollection<FaultViewModel> ActiveFaults { get; } = new();
        public ObservableCollection<ParameterViewModel> SelectedParameters { get; } = new();

        [ObservableProperty]
        private EquipmentViewModel selectedEquipment;

        // Свойства для отображения OEE выбранного оборудования
        [ObservableProperty]
        private string currentOeeText = "Нет данных";

        [ObservableProperty]
        private string currentAvailabilityText = "";

        [ObservableProperty]
        private string currentPerformanceText = "";

        [ObservableProperty]
        private string currentQualityText = "";

        public IRelayCommand StartMonitoringCommand { get; }
        public IRelayCommand StopMonitoringCommand { get; }
        public IRelayCommand<FaultViewModel> AcknowledgeFaultCommand { get; }
        public IRelayCommand GenerateReportCommand { get; }
        public IRelayCommand GenerateOeeReportCommand { get; }    // ✅ команда сводного отчёта
        public IRelayCommand<ParameterViewModel> ShowTrendCommand { get; }

        public MainViewModel(IEquipmentMonitor monitor,
                             IReportGenerator reportGenerator,
                             IDbContextFactory<AppDbContext> dbFactory,
                             IHistoryService historyService,
                             IOeeService oeeService)              // ✅
        {
            _monitor = monitor;
            _reportGenerator = reportGenerator;
            _dbFactory = dbFactory;
            _historyService = historyService;
            _oeeService = oeeService;

            _monitor.OnFaultDetected += OnFaultDetected;
            _monitor.OnStateChanged += OnStateChanged;

            StartMonitoringCommand = new RelayCommand(StartMonitoring);
            StopMonitoringCommand = new RelayCommand(StopMonitoring);
            AcknowledgeFaultCommand = new RelayCommand<FaultViewModel>(AcknowledgeFault);
            GenerateReportCommand = new RelayCommand(GenerateReport);
            GenerateOeeReportCommand = new RelayCommand(GenerateOeeReport);   
            ShowTrendCommand = new RelayCommand<ParameterViewModel>(ShowTrend);

            LoadEquipments();

            _refreshTimer = new System.Timers.Timer(1000);
            _refreshTimer.Elapsed += (s, e) => Application.Current.Dispatcher.Invoke(RefreshParametersAndOee);
            _refreshTimer.Start();
        }

        private void ShowTrend(ParameterViewModel? model)
        {
            throw new NotImplementedException();
        }

        private void GenerateReport()
        {
            throw new NotImplementedException();
        }

        private void OnStateChanged(int arg1, EquipmentState state)
        {
            throw new NotImplementedException();
        }

        private void OnFaultDetected(Fault fault)
        {
            throw new NotImplementedException();
        }

        private async void LoadEquipments() { }

        private void StartMonitoring() => _monitor.Start();
        private void StopMonitoring() => _monitor.Stop();

        // ... остальные методы ...

        private async void AcknowledgeFault(FaultViewModel faultVm)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var fault = await db.Faults.FindAsync(faultVm.Id);
            if (fault != null)
            {
                fault.Status = FaultStatus.Acknowledged;
                fault.EndTime = DateTime.Now;              // ✅ фиксируем время закрытия
                await db.SaveChangesAsync();

                bool hasActiveFaults = await db.Faults
                    .AnyAsync(f => f.EquipmentId == fault.EquipmentId && f.Status == FaultStatus.Active);
                if (!hasActiveFaults)
                {
                    var equipment = await db.Equipments.FindAsync(fault.EquipmentId);
                    if (equipment != null && equipment.CurrentState != EquipmentState.Normal)
                    {
                        equipment.CurrentState = EquipmentState.Normal;
                        await db.SaveChangesAsync();
                        OnStateChanged(equipment.Id, EquipmentState.Normal);
                    }
                }
            }
            faultVm.Status = FaultStatus.Acknowledged;
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(3));
                Application.Current.Dispatcher.Invoke(() => ActiveFaults.Remove(faultVm));
            });
        }

        private void GenerateOeeReport()
        {
            // Формируем сводный Excel-отчёт по OEE для всех единиц оборудования
            var dialog = new SaveFileDialog { Filter = "Excel Files|*.xlsx", FileName = "OeeReport.xlsx" };
            if (dialog.ShowDialog() != true) return;

            using var db = _dbFactory.CreateDbContext();
            var equipments = db.Equipments.ToList();
            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var ws = workbook.Worksheets.Add("ОЕЕ");
            ws.Cell(1, 1).Value = "Оборудование";
            ws.Cell(1, 2).Value = "Доступность";
            ws.Cell(1, 3).Value = "Производительность";
            ws.Cell(1, 4).Value = "Качество";
            ws.Cell(1, 5).Value = "ОEE";

            for (int i = 0; i < equipments.Count; i++)
            {
                var eq = equipments[i];
                // Для статического отчёта можем быстро посчитать OEE через сервис или вытащить уже кэшированное.
                // Используем OeeService (получим через DI, либо рассчитаем здесь). Для простоты вызовем синхронно, создав экземпляр OeeService с фабрикой.
                var oeeService = new EquipmentMonitoring.Core.Services.OeeService(_dbFactory);
                var result = oeeService.CalculateOeeAsync(eq.Id).GetAwaiter().GetResult();

                ws.Cell(i + 2, 1).Value = eq.Name;
                ws.Cell(i + 2, 2).Value = result.Availability;
                ws.Cell(i + 2, 3).Value = result.Performance;
                ws.Cell(i + 2, 4).Value = result.Quality;
                ws.Cell(i + 2, 5).Value = result.Oee;
            }

            ws.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            File.WriteAllBytes(dialog.FileName, stream.ToArray());
            MessageBox.Show("Сводный отчёт по ОЕЕ сохранён!", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void RefreshParametersAndOee()
        {
            if (SelectedEquipment != null)
            {
                // Обновление параметров
                using var db = _dbFactory.CreateDbContext();
                var dbParams = db.Parameters.Where(p => p.EquipmentId == SelectedEquipment.Id).ToList();
                foreach (var dbP in dbParams)
                {
                    var vm = SelectedParameters.FirstOrDefault(p => p.Id == dbP.Id);
                    if (vm != null)
                    {
                        vm.Value = dbP.Value;
                        vm.Timestamp = dbP.Timestamp;
                    }
                }
                // Обновление OEE для выбранного оборудования
                var oeeResult = await _oeeService.CalculateOeeAsync(SelectedEquipment.Id);
                if (oeeResult != null)
                {
                    CurrentOeeText = $"ОЕЕ: {oeeResult.Oee:P1}";
                    CurrentAvailabilityText = $"Доступность: {oeeResult.Availability:P1}";
                    CurrentPerformanceText = $"Производительность: {oeeResult.Performance:P1}";
                    CurrentQualityText = $"Качество: {oeeResult.Quality:P1}";
                }
            }
            else
            {
                CurrentOeeText = "Нет данных";
                CurrentAvailabilityText = "";
                CurrentPerformanceText = "";
                CurrentQualityText = "";
            }
        }

        // ... остальные методы (ShowTrend, LoadParametersForEquipment) ...
    }
}