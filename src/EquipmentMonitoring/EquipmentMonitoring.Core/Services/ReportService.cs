using ClosedXML.Excel;
using EquipmentMonitoring.Core.Data;
using EquipmentMonitoring.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;

namespace EquipmentMonitoring.Core.Services
{
    /// <summary>Генерация Excel-отчёта об отказах за период</summary>
    public class ReportService : IReportGenerator
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;
        public ReportService(IDbContextFactory<AppDbContext> contextFactory) => _contextFactory = contextFactory;

        public byte[] GenerateFaultReport(DateTime from, DateTime to)
        {
            using var db = _contextFactory.CreateDbContext();
            var faults = db.Faults.Include(f => f.Equipment)
                                  .Where(f => f.StartTime >= from && f.StartTime <= to)
                                  .OrderByDescending(f => f.StartTime)
                                  .ToList();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Отказы");
            // Заголовки
            ws.Cell(1, 1).Value = "Дата/время";
            ws.Cell(1, 2).Value = "Оборудование";
            ws.Cell(1, 3).Value = "Описание";
            ws.Cell(1, 4).Value = "Приоритет";
            ws.Cell(1, 5).Value = "Статус";

            // Заполнение данными
            for (int i = 0; i < faults.Count; i++)
            {
                var f = faults[i];
                ws.Cell(i + 2, 1).Value = f.StartTime.ToString("g");
                ws.Cell(i + 2, 2).Value = f.Equipment?.Name ?? "";
                ws.Cell(i + 2, 3).Value = f.Description;
                ws.Cell(i + 2, 4).Value = f.Priority.ToString();
                ws.Cell(i + 2, 5).Value = f.Status.ToString();
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}