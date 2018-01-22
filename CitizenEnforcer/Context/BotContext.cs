using CitizenEnforcer.Models;
using Microsoft.EntityFrameworkCore;

namespace CitizenEnforcer.Context
{
    public class BotContext : DbContext
    {
        public DbSet<Guild> Guilds { get; set; }
        public DbSet<RegisteredChannel> RegisteredChannels { get; set; }
        public DbSet<ModLog> ModLogs { get; set; }
        public DbSet<TempBan> TempBans { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=SCModBot.db");
        }
    }
}
