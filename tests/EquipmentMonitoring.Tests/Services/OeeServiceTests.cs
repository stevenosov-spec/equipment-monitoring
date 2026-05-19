using EquipmentMonitoring.Core.Data;
using EquipmentMonitoring.Core.Models;
using EquipmentMonitoring.Core.Services;
using EquipmentMonitoring.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EquipmentMonitoring.Tests.Services
{
    public class OeeServiceTests
    {
        private (AppDbContext db, OeeService service) CreateService()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var db = new AppDbContext(options);
            var factoryMock = new Mock<IDbContextFactory<AppDbContext>>();
            factoryMock.Setup(f => f.CreateDbContext()).Returns(db);
            factoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(db);
            var service = new OeeService(factoryMock.Object);
            return (db, service);
        }

        [Fact]
        public async Task CalculateOee_NoFaults_ReturnsFullAvailability()
        {
            var (db, service) = CreateService();
            var equipment = new Equipment { Name = "Насос" };
            db.Equipments.Add(equipment);
            await db.SaveChangesAsync();

            var result = await service.CalculateOeeAsync(equipment.Id, DateTime.Now.AddHours(-1), DateTime.Now);
            Assert.NotNull(result);
            Assert.Equal(1.0, result.Availability);
        }

        [Fact]
        public async Task CalculateOee_WithDowntime_ReducesAvailability()
        {
            var (db, service) = CreateService();
            var equipment = new Equipment { Name = "Насос" };
            db.Equipments.Add(equipment);
            await db.SaveChangesAsync();

            var fault = new Fault
            {
                EquipmentId = equipment.Id,
                StartTime = DateTime.Now.AddHours(-1),
                EndTime = DateTime.Now.AddMinutes(-30),
                Status = Core.Enums.FaultStatus.Acknowledged
            };
            db.Faults.Add(fault);
            await db.SaveChangesAsync();

            var result = await service.CalculateOeeAsync(equipment.Id, DateTime.Now.AddHours(-1), DateTime.Now);
            Assert.NotNull(result);
            Assert.Equal(0.5, result.Availability, 2);
        }
    }
}