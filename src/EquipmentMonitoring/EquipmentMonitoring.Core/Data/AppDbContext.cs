using Microsoft.EntityFrameworkCore;
using EquipmentMonitoring.Core.Models;

namespace EquipmentMonitoring.Core.Data
{
    /// <summary>Контекст базы данных Entity Framework</summary>
    public class AppDbContext : DbContext
    {
        public DbSet<Equipment> Equipments { get; set; }
        public DbSet<Parameter> Parameters { get; set; }
        public DbSet<Fault> Faults { get; set; }
        public DbSet<User> Users { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Каскадное удаление параметров при удалении оборудования
            modelBuilder.Entity<Equipment>()
                .HasMany(e => e.Parameters)
                .WithOne(p => p.Equipment)
                .HasForeignKey(p => p.EquipmentId)
                .OnDelete(DeleteBehavior.Cascade);

            // При удалении оборудования историю отказов сохраняем
            modelBuilder.Entity<Equipment>()
                .HasMany(e => e.Faults)
                .WithOne(f => f.Equipment)
                .HasForeignKey(f => f.EquipmentId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}