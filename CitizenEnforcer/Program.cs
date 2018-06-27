#region License
/*CitizenEnforcer - Moderation and logging bot
Copyright(C) 2018 Hawx
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
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using CitizenEnforcer.Common;
using CitizenEnforcer.Context;
using CitizenEnforcer.Services;
using CitizenEnforcer.Settings;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;

namespace CitizenEnforcer
{
    public class Program
    {
        public static async Task Main() => await new Program().Start();

        private DiscordSocketClient _client;
        private CommandService _cmd;
        private Configuration _config;
        public async Task Start()
        {
            Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Verbose,
                AlwaysDownloadUsers = true,
                MessageCacheSize = 200
            });
            _config = BuildConfig();
            FormatUtilities.Prefix = _config.Prefix;

            var services = ConfigureServices();
            await services.GetRequiredService<CommandHandler>().InitializeAsync();
            services.GetRequiredService<EditDeleteLogger>();
            services.GetRequiredService<TempBanTimer>();

            await _client.LoginAsync(TokenType.Bot, _config.Token);
            await _client.StartAsync();

            await Task.Delay(-1);
        }

        private IServiceProvider ConfigureServices()
        {
            Log.Logger = new LoggerConfiguration().WriteTo.Console().MinimumLevel.Verbose().CreateLogger();
            _cmd = new CommandService(new CommandServiceConfig
            {
                LogLevel = LogSeverity.Verbose,
                DefaultRunMode = RunMode.Async
            });
            _client.Log += LogConsole;
            _cmd.Log += LogConsole;
            
            return new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_cmd)
                .AddSingleton(_config)
                .AddSingleton<CommandHandler>()
                .AddSingleton<EditDeleteLogger>()
                .AddSingleton<InteractiveService>()
                .AddSingleton<LookupService>()
                .AddSingleton<ModerationService>()
                .AddSingleton<TempBanTimer>()
                .AddSingleton<Helper>()
                .AddDbContext<BotContext>(options =>
                {
                    if (string.IsNullOrWhiteSpace(_config?.DBPassword))
                        options.UseSqlite("Data Source=SCModBot.db");
                    else
                        options.UseSqlite(InitializeSQLiteConnection());
                }, ServiceLifetime.Transient)
                .AddMemoryCache()
                .BuildServiceProvider();
        }

        //My logging requirements for this project aren't complex so Serilog's global static logger will do fine
        private Task LogConsole(LogMessage logMessage)
        {
            Log.Write(EventLevelFromSeverity(logMessage.Severity), "{Source}: {Message}", logMessage.Source, logMessage.Exception?.ToString() ?? logMessage.Message);
            return Task.CompletedTask;
        }

        private Configuration BuildConfig()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config/configuration.json")
                .Build();
            //Don't kill me, but I prefer a binded config
            var configClass = new Configuration();
            config.Bind(configClass);
            Console.Write("Please enter the database password: ");
            configClass.DBPassword = GetPassword();
            return configClass;
        }

        public string GetPassword()
        {
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
                        pass = pass.Substring(0, pass.Length - 1);
                        Console.Write("\b \b");
                    }
                }
            }
            while (key.Key != ConsoleKey.Enter);
            Console.WriteLine();
            return pass;
        }

        private SqliteConnection InitializeSQLiteConnection()
        {
            var connection = new SqliteConnection("Data Source=SCModBot.db");
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT quote($password);";
            command.Parameters.AddWithValue("$password", _config.DBPassword);
            command.CommandText = "PRAGMA key = " + command.ExecuteScalar();
            command.Parameters.Clear();
            command.ExecuteNonQuery();
            return connection;
        }

        private static LogEventLevel EventLevelFromSeverity(LogSeverity severity) => (LogEventLevel)Math.Abs((int)severity - 5);
    }

}
