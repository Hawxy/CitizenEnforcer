using CitizenEnforcer.Models;
using CitizenEnforcer.Settings;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CitizenEnforcer.Context
{
    public class BotContext : DbContext
    {
        private readonly Configuration _configuration;
        public BotContext(Configuration configuration)
        {
            _configuration = configuration;
        }

        public DbSet<Guild> Guilds { get; set; }
        public DbSet<RegisteredChannel> RegisteredChannels { get; set; }
        public DbSet<ModLog> ModLogs { get; set; }
        public DbSet<TempBan> TempBans { get; set; }
        //Database encryption is required by Discord's bot ToS.
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (string.IsNullOrWhiteSpace(_configuration.DBPassword))
                optionsBuilder.UseSqlite("Data Source=SCModBot.db");
            else
                optionsBuilder.UseSqlite(InitializeSQLiteConnection());
        }

        private SqliteConnection InitializeSQLiteConnection()
        {
            var connection = new SqliteConnection("Data Source=SCModBot.db");
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT quote($password);";
            command.Parameters.AddWithValue("$password", _configuration.DBPassword);
            command.CommandText = "PRAGMA key = " + command.ExecuteScalar();
            command.Parameters.Clear();
            var result = command.ExecuteNonQuery();
            return connection;
        }
    }
}
