using CitizenEnforcer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CitizenEnforcer.Context
{
    public class BotContext : DbContext
    {
        public BotContext(DbContextOptions<BotContext> options) : base(options){ }

        public DbSet<Guild> Guilds { get; set; }
        public DbSet<RegisteredChannel> RegisteredChannels { get; set; }
        public DbSet<ModLog> ModLogs { get; set; }
        public DbSet<TempBan> TempBans { get; set; }
    }
    //Used for migrations
    public class BotContextFactory : IDesignTimeDbContextFactory<BotContext>
    {
        public BotContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<BotContext>().UseSqlite("Data Source = SCModBot.db");
            return new BotContext(optionsBuilder.Options);
        }
    }
}
