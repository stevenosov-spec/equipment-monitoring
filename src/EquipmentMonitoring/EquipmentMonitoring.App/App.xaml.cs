using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using EquipmentMonitoring.Core.Data;
using EquipmentMonitoring.Core.Services;
using EquipmentMonitoring.Core.Services.Interfaces;
using EquipmentMonitoring.Integration.Simulation;
using Microsoft.EntityFrameworkCore;
using EquipmentMonitoring.App.ViewModels;
using EquipmentMonitoring.App.Views;

namespace EquipmentMonitoring.App
{
    public partial class App : Application
    {
        public static IServiceProvider? Services { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();

            // ⚠️ Замените "ваш_пароль" на реальный пароль root MySQL
            string connectionString = "server=localhost;port=3306;database=equipment_monitor;user=root;password=1234";

            services.AddDbContextFactory<AppDbContext>(options =>
                options.UseMySql(connectionString,
                    new MySqlServerVersion(new Version(8, 0, 31)))
            );

            services.AddSingleton<ITagReader, SimulatedTagReader>();
            services.AddSingleton<IEquipmentMonitor, EquipmentMonitorService>();
            services.AddTransient<IReportGenerator, ReportService>();
            services.AddTransient<IHistoryService, HistoryService>();
            services.AddTransient<IOeeService, OeeService>();   // регистрация сервиса OEE

            services.AddSingleton<MainViewModel>();
            services.AddSingleton<MainWindow>();

            Services = services.BuildServiceProvider();

            using (var scope = Services.CreateScope())
            {
                var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
                using var db = factory.CreateDbContext();
                db.Database.Migrate();
                SeedData(db);
            }

            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.DataContext = Services.GetRequiredService<MainViewModel>();
            mainWindow.Show();
        }

        private void SeedData(AppDbContext db)
        {
            if (db.Equipments.Any()) return;   // Если уже есть данные – выходим

            // -------------------- Насос Р-102 --------------------
            var pump = new Core.Models.Equipment
            {
                Name = "Насос Р-102",
                Type = "Насос",
                Installation = "АВТ",
                CurrentState = Core.Enums.EquipmentState.NoData
            };
            db.Equipments.Add(pump);
            db.SaveChanges();   // получаем Id

            db.Parameters.AddRange(
                new Core.Models.Parameter
                {
                    Name = "Производительность",
                    TagAddress = "Flow_Pump101",
                    Unit = "м³/ч",
                    MinAllowed = 10,
                    MaxAllowed = 90,
                    NominalValue = 50,      // номинальная производительность для OEE
                    EquipmentId = pump.Id
                },
                new Core.Models.Parameter
                {
                    Name = "Температура",
                    TagAddress = "Temp_Pump101",
                    Unit = "°C",
                    MinAllowed = 30,
                    MaxAllowed = 70,
                    NominalValue = 45,      // номинальная температура
                    EquipmentId = pump.Id
                },
                new Core.Models.Parameter
                {
                    Name = "Давление на выходе",
                    TagAddress = "Pressure_Pump101",
                    Unit = "бар",
                    MinAllowed = 1,
                    MaxAllowed = 9,
                    NominalValue = 5,
                    EquipmentId = pump.Id
                },
                new Core.Models.Parameter
                {
                    Name = "Вибрация",
                    TagAddress = "Vibration_Pump101",
                    Unit = "мм/с",
                    MinAllowed = 0,
                    MaxAllowed = 4.5,
                    NominalValue = 2.25,
                    EquipmentId = pump.Id
                }
            );

            // -------------------- Печь П-201 --------------------
            var furnace = new Core.Models.Equipment
            {
                Name = "Печь П-201",
                Type = "Печь",
                Installation = "УПН",
                CurrentState = Core.Enums.EquipmentState.NoData
            };
            db.Equipments.Add(furnace);
            db.SaveChanges();

            db.Parameters.AddRange(
                new Core.Models.Parameter
                {
                    Name = "Температура на выходе",
                    TagAddress = "Temp_Furnace",
                    Unit = "°C",
                    MinAllowed = 800,
                    MaxAllowed = 1200,
                    NominalValue = 1000,
                    EquipmentId = furnace.Id
                },
                new Core.Models.Parameter
                {
                    Name = "Расход топлива",
                    TagAddress = "Flow_Fuel",
                    Unit = "кг/ч",
                    MinAllowed = 100,
                    MaxAllowed = 500,
                    NominalValue = 300,     // номинальный расход для расчёта производительности
                    EquipmentId = furnace.Id
                },
                new Core.Models.Parameter
                {
                    Name = "Давление в топке",
                    TagAddress = "Pressure_Furnace",
                    Unit = "Па",
                    MinAllowed = -50,
                    MaxAllowed = 50,
                    NominalValue = 0,
                    EquipmentId = furnace.Id
                }
            );

            // -------------------- Компрессор К-301 --------------------
            var compressor = new Core.Models.Equipment
            {
                Name = "Компрессор К-301",
                Type = "Компрессор",
                Installation = "ГКС",
                CurrentState = Core.Enums.EquipmentState.NoData
            };
            db.Equipments.Add(compressor);
            db.SaveChanges();

            db.Parameters.AddRange(
                new Core.Models.Parameter
                {
                    Name = "Температура нагнетания",
                    TagAddress = "Temp_Compressor",
                    Unit = "°C",
                    MinAllowed = 60,
                    MaxAllowed = 110,
                    NominalValue = 85,
                    EquipmentId = compressor.Id
                },
                new Core.Models.Parameter
                {
                    Name = "Давление на входе",
                    TagAddress = "Pressure_CompIn",
                    Unit = "бар",
                    MinAllowed = 1,
                    MaxAllowed = 8,
                    NominalValue = 4.5,
                    EquipmentId = compressor.Id
                },
                new Core.Models.Parameter
                {
                    Name = "Вибрация",
                    TagAddress = "Vibration_Comp",
                    Unit = "мм/с",
                    MinAllowed = 0,
                    MaxAllowed = 7.1,
                    NominalValue = 3.5,
                    EquipmentId = compressor.Id
                }
            );

            db.SaveChanges();
        }
    }
}