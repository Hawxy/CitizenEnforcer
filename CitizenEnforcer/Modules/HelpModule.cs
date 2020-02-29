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
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CitizenEnforcer.Services;
using Discord;
using Discord.Addons.Hosting.Reliability;
using Discord.Addons.Interactive;
using Discord.Commands;
using Microsoft.Extensions.Hosting;

namespace CitizenEnforcer.Modules
{
    public class HelpModule : ModuleBase<SocketCommandContext>
    {
        private readonly Helper _help;
        private readonly InteractiveService _interactiveService;
        private readonly IHost _host;

        public HelpModule(Helper help, InteractiveService interactiveService, IHost host)
        {
            _help = help;
            _interactiveService = interactiveService;
            _host = host;
        }


        [Command("help")]
        [Summary("Displays list of available commands")]
        [RequireContext(ContextType.Guild)]
        public async Task Help()
        {
            await _interactiveService.ReplyAndDeleteAsync(Context, "Sending you list of my commands now!", timeout: TimeSpan.FromSeconds(10));
            await _help.HelpAsync(Context);
        }
        [Command("help")]
        [Summary("Shows information about a command")]
        public Task Help([Remainder]string command) => _help.HelpAsync(Context, command);

        [Command("info")]
        [Alias("stats")]
        [Summary("Provides information about the bot")]
        public async Task Info()
        {
            await ReplyAsync(
                $"{Format.Bold("Info")}\n" +
                "- Developed by Hawx#0001\n" +
                "- Github: `https://github.com/Hawxy/CitizenEnforcer` \n" +
                $"- Library: Discord.Net ({DiscordConfig.Version})\n" +
                $"- Runtime: {RuntimeInformation.FrameworkDescription} {RuntimeInformation.OSArchitecture}\n" +
                $"- Uptime: {GetUptime()}\n\n" +

                $"{Format.Bold("Stats")}\n" +
                $"- Heap Size: {GetHeapSize()} MB\n" +
                $"- Guilds: {Context.Client.Guilds.Count}\n" +
                $"- Channels: {Context.Client.Guilds.Sum(g => g.Channels.Count)}\n" +
                $"- Users: {Context.Client.Guilds.Sum(g => g.MemberCount)}" 
            );
            static string GetUptime()=> (DateTime.Now - Process.GetCurrentProcess().StartTime).ToString(@"dd\.hh\:mm\:ss");
            static string GetHeapSize() => Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2).ToString(CultureInfo.CurrentCulture);
        }

        [Command("shutdown")]
        [RequireOwner]
        public async Task Shutdown()
        {
            await ReplyAsync("Goodbye!");
            await _host.StopReliablyAsync();
        }
    }
}
