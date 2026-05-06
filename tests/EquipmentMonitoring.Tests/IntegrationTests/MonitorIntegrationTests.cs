using EquipmentMonitoring.Core.Data;
using EquipmentMonitoring.Core.Enums;
using EquipmentMonitoring.Core.Models;
using EquipmentMonitoring.Core.Services;
using EquipmentMonitoring.Integration.Simulation;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Threading;
using Xunit;

namespace EquipmentMonitoring.Tests.IntegrationTests
{
    public class MonitorIntegrationTests
    {
        [Fact]
        public void SimulatorToMonitor_CreatesFaultWhenOutOfRange()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var db = new AppDbContext(options);
            var equipment = new Equipment { Name = "Тестовый насос", CurrentState = EquipmentState.Normal };
            db.Equipments.Add(equipment);
            var param = new Parameter
            {
                Name = "Давление",
                TagAddress = "Pressure_Pump101",
                MinAllowed = 0,
                MaxAllowed = 5,
                Equipment = equipment
            };
            db.Parameters.Add(param);
            db.SaveChanges();

            var factoryMock = new Mock<IDbContextFactory<AppDbContext>>();
            factoryMock.Setup(f => f.CreateDbContext()).Returns(db);

            var simulator = new SimulatedTagReader();
            // Заменим внутренний словарь тестов через рефлексию, чтобы гарантированно выходить за границы
            var newTags = new System.Collections.Generic.Dictionary<string, (double min, double max, double avg)>
            {
                { "Pressure_Pump101", (10, 20, 15) }
            };
            typeof(SimulatedTagReader)
                .GetField("_tags", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(simulator, newTags);

            var monitor = new EquipmentMonitorService(simulator, factoryMock.Object);
            Fault detectedFault = null;
            monitor.OnFaultDetected += f => detectedFault = f;

            monitor.Start();
            Thread.Sleep(1500);
            monitor.Stop();

            Assert.NotNull(detectedFault);
            Assert.Equal(equipment.Id, detectedFault.EquipmentId);
        }
    }
}