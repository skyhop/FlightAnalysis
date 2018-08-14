using Boerman.FlightAnalysis.Models;
using Microsoft.EntityFrameworkCore;

namespace PersistentFlightContext
{
    public class Database : DbContext
    {
        public DbSet<FlightMetadata> Flights { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=flights.db");
        }
    }
}
