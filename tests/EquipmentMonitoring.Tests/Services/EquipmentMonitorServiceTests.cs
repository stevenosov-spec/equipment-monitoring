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
        private AppDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public void TagValueChanged_WhenValueOutOfRange_CreatesFault()
        {
            // Arrange
            using var db = CreateInMemoryContext();
            var equipment = new Equipment { Name = "Насос", CurrentState = EquipmentState.Normal };
            var parameter = new Parameter
            {
                Name = "Давление",
                TagAddress = "Pressure",
                MinAllowed = 1,
                MaxAllowed = 5,
                Equipment = equipment
            };
            db.Equipments.Add(equipment);
            db.Parameters.Add(parameter);
            db.SaveChanges();

            var tagReaderMock = new Mock<ITagReader>();
            var factoryMock = new Mock<IDbContextFactory<AppDbContext>>();
            factoryMock.Setup(f => f.CreateDbContext()).Returns(db);

            var monitor = new EquipmentMonitorService(tagReaderMock.Object, factoryMock.Object);
            Fault detectedFault = null;
            monitor.OnFaultDetected += f => detectedFault = f;

            // Act
            tagReaderMock.Raise(t => t.TagValueChanged += null,
                new TagValueChangedEventArgs { TagAddress = "Pressure", Value = 10.0, Timestamp = DateTime.Now });

            // Assert
            Assert.NotNull(detectedFault);
            Assert.Equal(equipment.Id, detectedFault.EquipmentId);
            Assert.Equal(FaultStatus.Active, detectedFault.Status);
            Assert.Single(db.Faults.ToList());
        }

        [Fact]
        public void TagValueChanged_WhenValueInRange_DoesNotCreateFault()
        {
            // Arrange
            using var db = CreateInMemoryContext();
            var equipment = new Equipment { Name = "Насос", CurrentState = EquipmentState.Normal };
            var parameter = new Parameter
            {
                Name = "Давление",
                TagAddress = "Pressure",
                MinAllowed = 1,
                MaxAllowed = 5,
                Equipment = equipment
            };
            db.Equipments.Add(equipment);
            db.Parameters.Add(parameter);
            db.SaveChanges();

            var tagReaderMock = new Mock<ITagReader>();
            var factoryMock = new Mock<IDbContextFactory<AppDbContext>>();
            factoryMock.Setup(f => f.CreateDbContext()).Returns(db);

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