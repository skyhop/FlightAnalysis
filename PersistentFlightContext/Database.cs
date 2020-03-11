using Microsoft.EntityFrameworkCore;
using Skyhop.FlightAnalysis.Models;

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
