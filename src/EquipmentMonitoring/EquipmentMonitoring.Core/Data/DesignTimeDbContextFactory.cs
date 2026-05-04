using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EquipmentMonitoring.Core.Data
{
    /// <summary>Фабрика для создания контекста во время разработки (миграции)</summary>
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            // ⚠️ Пароль должен совпадать с тем, что в App.xaml.cs
            var connectionString = "server=localhost;port=3306;database=equipment_monitor;user=root;password=1234";
            optionsBuilder.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 31)));
            return new AppDbContext(optionsBuilder.Options);
        }
    }
}