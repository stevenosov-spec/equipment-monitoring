using EquipmentMonitoring.Core.Data;
using EquipmentMonitoring.Core.Models;
using EquipmentMonitoring.Core.Services;
using EquipmentMonitoring.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using Xunit;

namespace EquipmentMonitoring.Tests.Services
{
    public class ReportServiceTests
    {
        [Fact]
        public void GenerateFaultReport_ReturnsValidExcel()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var db = new AppDbContext(options);
            var equipment = new Equipment { Name = "Печь" };
            var fault = new Fault
            {
                Equipment = equipment,
                StartTime = DateTime.Now,
                Description = "Тест",
                Priority = Core.Enums.FaultPriority.High,
                Status = Core.Enums.FaultStatus.Active
            };
            db.Faults.Add(fault);
            db.SaveChanges();

            var factoryMock = new Mock<IDbContextFactory<AppDbContext>>();
            factoryMock.Setup(f => f.CreateDbContext()).Returns(db);
            var service = new ReportService(factoryMock.Object);

            byte[] reportBytes = service.GenerateFaultReport(DateTime.Now.AddDays(-1), DateTime.Now.AddDays(1));
            Assert.NotNull(reportBytes);
            Assert.True(reportBytes.Length > 0);
        }
    }
}