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

using System;
using System.Threading.Tasks;
using CacheManager.Core;
using CitizenEnforcer.Common;
using CitizenEnforcer.Context;
using CitizenEnforcer.Services;
using Discord;
using Discord.Addons.Hosting;
using Discord.Addons.Hosting.Reliability;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using EFSecondLevelCache.Core;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sentry;
using Serilog;
using Serilog.Events;

namespace CitizenEnforcer
{
    public class Program
    {
        public static async Task Main()
        {
            string dbPassword = GetPassword(); 

            var builder = new HostBuilder()
                .ConfigureAppConfiguration(x =>
                {
                    x.AddJsonFile("config/configuration.json");
                })
                .UseSerilog((context, config) =>
                {
                    config
                        .WriteTo.Console()
                        .MinimumLevel.Verbose()
                        .MinimumLevel.Override("Microsoft", LogEventLevel.Error);

                    if (!string.IsNullOrEmpty(context.Configuration["Sentry"]))
                        config.WriteTo.Sentry(o =>
                        {
                            o.Dsn = new Dsn(context.Configuration["Sentry"]);
                            o.MinimumBreadcrumbLevel = LogEventLevel.Debug;
                            o.MinimumEventLevel = LogEventLevel.Error;
                        });

                })
                .ConfigureDiscordHost<DiscordSocketClient>((context, discordBuilder) =>
                {
                    var config = new DiscordSocketConfig
                    {
                        LogLevel = LogSeverity.Info,
                        AlwaysDownloadUsers = true,
                        MessageCacheSize = 200
                    };

                    discordBuilder.SetDiscordConfiguration(config);
                    discordBuilder.SetToken(context.Configuration["Token"]);
                })
                .UseCommandService((context, config) =>
                {
                    config.LogLevel = LogSeverity.Verbose;
                    config.DefaultRunMode = RunMode.Async;
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddHostedService<CommandHandler>()
                        .AddHostedService<EditDeleteLogger>()
                        .AddHostedService<UserUpdatedLogger>()
                        .AddHostedService<TempBanTimer>()
                        .AddSingleton<InteractiveService>()
                        .AddSingleton<LookupService>()
                        .AddSingleton<ModerationService>()
                        .AddSingleton<Helper>()
                        .AddDbContext<BotContext>(options =>
                        {
                            if (string.IsNullOrWhiteSpace(dbPassword))
                                options.UseSqlite("Data Source=SCModBot.db");
                            else
                                options.UseSqlite(InitializeSQLiteConnection(dbPassword));
                        }, ServiceLifetime.Transient)
                        .AddMemoryCache()
                        .AddEFSecondLevelCache()
                        .AddSingleton(typeof(ICacheManager<>), typeof(BaseCacheManager<>))
                        .AddSingleton(typeof(ICacheManagerConfiguration),
                            new CacheManager.Core.ConfigurationBuilder()
                                .WithJsonSerializer()
                                .WithMicrosoftMemoryCacheHandle("MemoryCache")
                                .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMinutes(30))
                                .DisablePerformanceCounters()
                                .DisableStatistics()
                                .Build());
                    //TODO Replace with singleton/factory at some point
                    ModeratorFormats.Prefix = context.Configuration["Prefix"];
                });

            var host = builder.Build();
            using (host)
            {
                await host.RunAsync();
            }
        }
        public static string GetPassword()
        {
            Console.Write("Please enter the database password: ");
            ConsoleKeyInfo key;
            string pass = "";
            do
            {
                key = Console.ReadKey(true);
                if (!char.IsControl(key.KeyChar))
                {
                    pass += key.KeyChar;
                    Console.Write("*");
                }
                else
                {
                    if (key.Key == ConsoleKey.Backspace && pass.Length > 0)
                    {
                        pass = pass[..^1];
                        Console.Write("\b \b");
                    }
                }
            }
            while (key.Key != ConsoleKey.Enter);
            Console.WriteLine();
            return pass;
        }

        private static SqliteConnection InitializeSQLiteConnection(string password)
        {
            var connection = new SqliteConnection("Data Source=SCModBot.db");
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT quote($password);";
            command.Parameters.AddWithValue("$password", password);
            command.CommandText = "PRAGMA key = " + command.ExecuteScalar();
            command.Parameters.Clear();
            command.ExecuteNonQuery();
            return connection;
        }
    }

}
