using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentMonitoring.Core.Data;
using EquipmentMonitoring.Core.Enums;
using EquipmentMonitoring.Core.Models;
using EquipmentMonitoring.Core.Services;            // <-- для OeeService
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
        private readonly IOeeService _oeeService;
        private readonly System.Timers.Timer _refreshTimer;

        public ObservableCollection<EquipmentViewModel> Equipments { get; } = new();
        public ObservableCollection<FaultViewModel> ActiveFaults { get; } = new();
        public ObservableCollection<ParameterViewModel> SelectedParameters { get; } = new();

        [ObservableProperty]
        private EquipmentViewModel? selectedEquipment;

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
        public IRelayCommand GenerateOeeReportCommand { get; }
        public IRelayCommand<ParameterViewModel> ShowTrendCommand { get; }

        public MainViewModel(IEquipmentMonitor monitor,
                             IReportGenerator reportGenerator,
                             IDbContextFactory<AppDbContext> dbFactory,
                             IHistoryService historyService,
                             IOeeService oeeService)
        {
            _monitor = monitor;
            _reportGenerator = reportGenerator;
            _dbFactory = dbFactory;
            _historyService = historyService;
            _oeeService = oeeService;
            _refreshTimer = new System.Timers.Timer(1000);

            _monitor.OnFaultDetected += OnFaultDetected;
            _monitor.OnStateChanged += OnStateChanged;

            StartMonitoringCommand = new RelayCommand(StartMonitoring);
            StopMonitoringCommand = new RelayCommand(StopMonitoring);
            AcknowledgeFaultCommand = new RelayCommand<FaultViewModel>(AcknowledgeFault);
            GenerateReportCommand = new RelayCommand(GenerateReport);
            GenerateOeeReportCommand = new RelayCommand(GenerateOeeReport);
            ShowTrendCommand = new RelayCommand<ParameterViewModel>(ShowTrend);

            LoadEquipments();

            _refreshTimer.Elapsed += (s, e) => Application.Current.Dispatcher.Invoke(RefreshParametersAndOee);
            _refreshTimer.Start();
        }

        private async void LoadEquipments()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var list = await db.Equipments.ToListAsync();
            foreach (var eq in list) Equipments.Add(new EquipmentViewModel(eq));
        }

        private void StartMonitoring() => _monitor.Start();
        private void StopMonitoring() => _monitor.Stop();

        private void OnStateChanged(int equipmentId, EquipmentState newState)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var vm = Equipments.FirstOrDefault(e => e.Id == equipmentId);
                if (vm != null) vm.State = newState;
            });
        }

        private void OnFaultDetected(Fault fault)
        {
            Application.Current.Dispatcher.Invoke(() => ActiveFaults.Insert(0, new FaultViewModel(fault)));
        }

        private async void AcknowledgeFault(FaultViewModel? faultVm)
        {
            if (faultVm == null) return;

            await using var db = await _dbFactory.CreateDbContextAsync();
            var fault = await db.Faults.FindAsync(faultVm.Id);
            if (fault != null)
            {
                fault.Status = FaultStatus.Acknowledged;
                fault.EndTime = DateTime.Now;
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

        private void GenerateReport()
        {
            var data = _reportGenerator.GenerateFaultReport(DateTime.Now.AddDays(-7), DateTime.Now);
            var dialog = new SaveFileDialog { Filter = "Excel Files|*.xlsx", FileName = "FaultReport.xlsx" };
            if (dialog.ShowDialog() == true) File.WriteAllBytes(dialog.FileName, data);
        }

        /// <summary>
        /// Асинхронный метод генерации сводного отчёта OEE за произвольный период.
        /// Не блокирует интерфейс.
        /// </summary>
        private async void GenerateOeeReport()
        {
            // 1. Диалог выбора дат
            var dateDialog = new OeeDateRangeDialog();
            dateDialog.Owner = Application.Current.MainWindow;
            if (dateDialog.ShowDialog() != true) return;

            DateTime start = dateDialog.StartDate;
            DateTime end = dateDialog.EndDate;

            // 2. Путь для сохранения Excel
            var saveDialog = new SaveFileDialog { Filter = "Excel Files|*.xlsx", FileName = "OeeReport.xlsx" };
            if (saveDialog.ShowDialog() != true) return;

            // 3. Получаем список оборудования
            using var db = _dbFactory.CreateDbContext();
            var equipments = db.Equipments.ToList();

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var ws = workbook.Worksheets.Add("ОЕЕ");
            ws.Cell(1, 1).Value = "Оборудование";
            ws.Cell(1, 2).Value = "Начало периода";
            ws.Cell(1, 3).Value = "Конец периода";
            ws.Cell(1, 4).Value = "Доступность";
            ws.Cell(1, 5).Value = "Производительность";
            ws.Cell(1, 6).Value = "Качество";
            ws.Cell(1, 7).Value = "OEE";

            var oeeCalc = new OeeService(_dbFactory);   // можно использовать DI-сервис _oeeService, но он завязан на IOeeService, а нам нужен конкретный метод с датами

            for (int i = 0; i < equipments.Count; i++)
            {
                var eq = equipments[i];
                // Асинхронный вызов без блокировки UI
                var result = await oeeCalc.CalculateOeeAsync(eq.Id, start, end);
                if (result != null)
                {
                    ws.Cell(i + 2, 1).Value = eq.Name;
                    ws.Cell(i + 2, 2).Value = start.ToString("g");
                    ws.Cell(i + 2, 3).Value = end.ToString("g");
                    ws.Cell(i + 2, 4).Value = result.Availability;
                    ws.Cell(i + 2, 5).Value = result.Performance;
                    ws.Cell(i + 2, 6).Value = result.Quality;
                    ws.Cell(i + 2, 7).Value = result.Oee;
                }
            }

            ws.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            await File.WriteAllBytesAsync(saveDialog.FileName, stream.ToArray());

            MessageBox.Show("Сводный отчёт по ОЕЕ сохранён!", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowTrend(ParameterViewModel? paramVm)
        {
            if (paramVm == null) return;

            var trendVm = new TrendViewModel(
                _historyService,
                paramVm.Id,
                paramVm.Name,
                paramVm.MinAllowed,
                paramVm.MaxAllowed);

            var window = new TrendWindow
            {
                DataContext = trendVm,
                Owner = Application.Current.MainWindow
            };
            window.ShowDialog();
        }

        partial void OnSelectedEquipmentChanged(EquipmentViewModel? value)
        {
            if (value != null) LoadParametersForEquipment(value.Id);
        }

        private async void LoadParametersForEquipment(int equipmentId)
        {
            SelectedParameters.Clear();
            await using var db = await _dbFactory.CreateDbContextAsync();
            var parameters = await db.Parameters.Where(p => p.EquipmentId == equipmentId).ToListAsync();
            foreach (var p in parameters) SelectedParameters.Add(new ParameterViewModel(p));
        }

        private async void RefreshParametersAndOee()
        {
            if (SelectedEquipment != null)
            {
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

                var oeeResult = await _oeeService.CalculateOeeAsync(SelectedEquipment.Id, DateTime.Now.AddHours(-1), DateTime.Now);
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
    }
}