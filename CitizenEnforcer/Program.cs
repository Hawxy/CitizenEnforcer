using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using CitizenEnforcer.Context;
using CitizenEnforcer.Services;
using CitizenEnforcer.Settings;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
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
                MessageCacheSize = 1000
            });
            _config = BuildConfig();

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
                .AddDbContext<BotContext>(ServiceLifetime.Transient)
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
        private static LogEventLevel EventLevelFromSeverity(LogSeverity severity) => (LogEventLevel)Math.Abs((int)severity - 5);
    }

}
