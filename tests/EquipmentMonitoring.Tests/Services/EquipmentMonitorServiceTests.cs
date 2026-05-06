using EquipmentMonitoring.Core.Data;
using EquipmentMonitoring.Core.Enums;
using EquipmentMonitoring.Core.Models;
using EquipmentMonitoring.Core.Services;
using EquipmentMonitoring.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Linq;
using Xunit;

namespace EquipmentMonitoring.Tests.Services
{
    public class EquipmentMonitorServiceTests
    {
        private AppDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public void TagValueChanged_WhenValueOutOfRange_CreatesFault()
        {
            // Arrange
            string dbName = Guid.NewGuid().ToString();

            // Подготавливаем данные в отдельном контексте
            using (var setupDb = CreateContext(dbName))
            {
                var equipment = new Equipment { Name = "Насос", CurrentState = EquipmentState.Normal };
                var parameter = new Parameter
                {
                    Name = "Давление",
                    TagAddress = "Pressure",
                    MinAllowed = 1,
                    MaxAllowed = 5,
                    Equipment = equipment
                };
                setupDb.Equipments.Add(equipment);
                setupDb.Parameters.Add(parameter);
                setupDb.SaveChanges();
            }

            var tagReaderMock = new Mock<ITagReader>();
            var factoryMock = new Mock<IDbContextFactory<AppDbContext>>();

            // Фабрика создаёт новый контекст при каждом вызове
            factoryMock.Setup(f => f.CreateDbContext()).Returns(() => CreateContext(dbName));

            var monitor = new EquipmentMonitorService(tagReaderMock.Object, factoryMock.Object);
            Fault detectedFault = null;
            monitor.OnFaultDetected += f => detectedFault = f;

            // Act
            tagReaderMock.Raise(t => t.TagValueChanged += null,
                new TagValueChangedEventArgs { TagAddress = "Pressure", Value = 10.0, Timestamp = DateTime.Now });

            // Assert
            Assert.NotNull(detectedFault);
            Assert.Equal(FaultStatus.Active, detectedFault.Status);

            // Проверяем сохранение отказа в БД через новый контекст
            using (var checkDb = CreateContext(dbName))
            {
                var faults = checkDb.Faults.ToList();
                Assert.Single(faults);
            }
        }

        [Fact]
        public void TagValueChanged_WhenValueInRange_DoesNotCreateFault()
        {
            // Arrange
            string dbName = Guid.NewGuid().ToString();

            using (var setupDb = CreateContext(dbName))
            {
                var equipment = new Equipment { Name = "Насос", CurrentState = EquipmentState.Normal };
                var parameter = new Parameter
                {
                    Name = "Давление",
                    TagAddress = "Pressure",
                    MinAllowed = 1,
                    MaxAllowed = 5,
                    Equipment = equipment
                };
                setupDb.Equipments.Add(equipment);
                setupDb.Parameters.Add(parameter);
                setupDb.SaveChanges();
            }

            var tagReaderMock = new Mock<ITagReader>();
            var factoryMock = new Mock<IDbContextFactory<AppDbContext>>();
            factoryMock.Setup(f => f.CreateDbContext()).Returns(() => CreateContext(dbName));

            var monitor = new EquipmentMonitorService(tagReaderMock.Object, factoryMock.Object);
            bool faultDetected = false;
            monitor.OnFaultDetected += f => faultDetected = true;

            // Act
            tagReaderMock.Raise(t => t.TagValueChanged += null,
                new TagValueChangedEventArgs { TagAddress = "Pressure", Value = 3.0, Timestamp = DateTime.Now });

            Assert.False(faultDetected);
        }
    }
}