    using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

    namespace Kursovaiya_Yakovlev
    {
        public class DatabaseContext : DbContext
        {
        public DbSet<Service> Service { get; set; }
        public DbSet<Staff> Staff { get; set; }
        public DbSet<Status> Status { get; set; }
        public DbSet<Transactions> Transactions { get; set; }
        public DbSet<Users> Users { get; set; }
        public DbSet<HouseData> HouseData { get; set; }
        public DbSet<AccessRights> AccessRights { get; set; }


        public static DatabaseContext _context;

            public DatabaseContext() { }

            public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) { }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                optionsBuilder.UseNpgsql(@"Host=172.20.7.53;Database=db3996_20;Username=root;Password=root");
            }

            public static DatabaseContext GetContext()
            {
                var context = new DatabaseContext();
                context.Database.EnsureCreated(); 
                return context;
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                // Настройка таблицы HouseData
                modelBuilder.Entity<HouseData>(entity =>
                {
                    entity.ToTable("house_data", "dbpr");
             
                });

               
                modelBuilder.Entity<Transactions>(entity =>
                {
                    entity.ToTable("transactions", "dbpr");

                    entity.HasOne(t => t.Property)
                        .WithMany(h => h.Transactions)
                        .HasForeignKey(t => t.PropertyId)
                        .OnDelete(DeleteBehavior.Restrict);

                    entity.HasOne(t => t.Service)
                        .WithMany()
                        .HasForeignKey(t => t.ServiceId)
                        .OnDelete(DeleteBehavior.Restrict);

                    entity.HasOne(t => t.Owner)
                        .WithMany()
                        .HasForeignKey(t => t.OwnerId)
                        .OnDelete(DeleteBehavior.Restrict);

                    entity.HasOne(t => t.Client)
                        .WithMany()
                        .HasForeignKey(t => t.ClientId)
                        .OnDelete(DeleteBehavior.Restrict);

                    entity.HasOne(t => t.Status)
                        .WithMany()
                        .HasForeignKey(t => t.StatusId)
                        .OnDelete(DeleteBehavior.Restrict);
                });

                // Настройка таблицы Service
                modelBuilder.Entity<Service>(entity =>
                {
                    entity.ToTable("service", "dbpr");
                    entity.HasOne(s => s.Staff)
                        .WithMany()
                        .HasForeignKey(s => s.StaffId);
                });

                // Настройка таблицы Users
                modelBuilder.Entity<Users>(entity =>
                {
                    entity.ToTable("users", "dbpr");
                });

                // Настройка таблицы Status
                modelBuilder.Entity<Status>(entity =>
                {
                    entity.ToTable("status", "dbpr");
                });
            }
        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Properties<DateTime>()
                .HaveConversion<DateTimeToLocalConverter>();
        }

        public class DateTimeToLocalConverter : ValueConverter<DateTime, DateTime>
        {
            public DateTimeToLocalConverter()
                : base(
                    v => v.Kind == DateTimeKind.Utc ? v.ToLocalTime() : v,
                    v => DateTime.SpecifyKind(v, DateTimeKind.Unspecified))
            {
            }
        }
    }
    }