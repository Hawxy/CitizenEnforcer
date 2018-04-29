using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CitizenEnforcer.Services;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;

namespace CitizenEnforcer.Modules
{
    public class HelpModule : ModuleBase<SocketCommandContext>
    {
        public Helper _helpservice { get; set; }
        public InteractiveService _interactiveService { get; set; }

        [Command("help")]
        [Summary("Displays list of available commands")]
        [RequireContext(ContextType.Guild)]
        public async Task Help()
        {
            await _interactiveService.ReplyAndDeleteAsync(Context, "Sending you list of my commands now!", timeout: TimeSpan.FromSeconds(10));
            await _helpservice.HelpAsync(Context);
        }
        [Command("help")]
        [Summary("Shows information about a command")]
        public async Task Help([Remainder]string command)
        {
            await _helpservice.HelpAsync(Context, command);
        }

        [Command("info")]
        [Alias("stats")]
        [Summary("Provides information about the bot")]
        public async Task Info()
        {
            await ReplyAsync(
                $"{Format.Bold("Info")}\n" +
                "- Developed by Hawx\n" +
                "- Github: `https://github.com/Hawxy/CitizenEnforcer` \n" +
                $"- Library: Discord.Net ({DiscordConfig.Version})\n" +
                $"- Runtime: .NET Core 2.0 {RuntimeInformation.OSArchitecture}\n" +
                $"- Uptime: {GetUptime()}\n\n" +

                $"{Format.Bold("Stats")}\n" +
                $"- Heap Size: {GetHeapSize()} MB\n" +
                $"- Guilds: {Context.Client.Guilds.Count}\n" +
                $"- Channels: {Context.Client.Guilds.Sum(g => g.Channels.Count)}\n" +
                $"- Users: {Context.Client.Guilds.Sum(g => g.MemberCount)}" 
            );
            string GetUptime()=> (DateTime.Now - Process.GetCurrentProcess().StartTime).ToString(@"dd\.hh\:mm\:ss");
            string GetHeapSize() => Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2).ToString(CultureInfo.CurrentCulture);
        }

        [Command("shutdown")]
        [RequireOwner]
        public async Task StopBot()
        {
            await ReplyAsync("Goodbye!");
            await Context.Client.StopAsync();
            Environment.Exit(0);
        }
    }
}
