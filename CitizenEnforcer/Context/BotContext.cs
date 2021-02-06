#region License
/*CitizenEnforcer - Moderation and logging bot
Copyright(C) 2018-2020 Hawx
https://github.com/Hawxy/CitizenEnforcer

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published
by the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program.If not, see http://www.gnu.org/licenses/ */
#endregion

using System.Linq;
using CitizenEnforcer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.ValueGeneration.Internal;

namespace CitizenEnforcer.Context
{
    public class BotContext : DbContext
    {
        public BotContext(DbContextOptions<BotContext> options) : base(options){ }

        public DbSet<Guild> Guilds { get; set; }
        public DbSet<RegisteredChannel> RegisteredChannels { get; set; }
        public DbSet<ModLog> ModLogs { get; set; }
        public DbSet<TempBan> TempBans { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ModLog>()
                .HasOne(m => m.TempBan)
                .WithOne(i => i.ModLog)
                .HasForeignKey<TempBan>(p => p.ModLogDBId);

            foreach (var property in modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(ulong)))
            {
                property.SetValueConverter(
                    new ValueConverter<ulong, long>(
                        convertToProviderExpression: ulongValue => (long)ulongValue,
                        convertFromProviderExpression: longValue => (ulong)longValue,
                        mappingHints: new ConverterMappingHints(valueGeneratorFactory: (p, t) => new TemporaryULongValueGenerator())
                    ));
            }
        }
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
