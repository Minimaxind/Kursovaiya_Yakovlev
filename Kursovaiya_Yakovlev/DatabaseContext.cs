using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Kursovaiya_Yakovlev
{
    public class DatabaseContext : DbContext
    {

        public DbSet<Users> users { get; set; }

        public static DatabaseContext _context;

        public DatabaseContext() { }

        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(@"Host=172.20.7.53;Database=db3996_20;Username=root;Password=root");
        }

        public static DatabaseContext GetContext()
        {
            if (_context == null) _context = new DatabaseContext();
            return _context;
        }
    }
}
