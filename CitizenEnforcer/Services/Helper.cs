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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Net;
using Microsoft.Extensions.Configuration;

namespace CitizenEnforcer.Services
{
    public class Helper
    {
        private readonly CommandService _service;
        private readonly IServiceProvider _map;
        private readonly IConfiguration _config;

        public Helper(CommandService service, IServiceProvider map, IConfiguration config)
        {
            _service = service;
            _map = map;
            _config = config;
        }

        public async Task HelpAsync(SocketCommandContext context)
        {
            string prefix = _config["Prefix"];
            var builder = new EmbedBuilder
            {
                Color = new Color(114, 137, 218),
                Description = $"These are the commands you have access to on the guild **{context.Guild.Name}**.\nAvailable commands differ based on your guild permissions.\nFor more information, use {_config["Prefix"]}help [command]"
            };

            foreach (var module in _service.Modules)
            {
                if(module.Name == "HelpModule")
                    continue;
                StringBuilder description = new StringBuilder();
                foreach (var cmd in module.Commands)
                {
                    var result = await cmd.CheckPreconditionsAsync(context, _map);
                    if (result.IsSuccess)
                    {
                        description.Append($"{prefix}{cmd.Aliases.First()} ");
                        if (cmd.Parameters.Any())
                            description.Append($"[{string.Join(", ", cmd.Parameters.Select(p => p.Name))}]");
                        description.Append("\n");
                    }
                }
                if (!string.IsNullOrWhiteSpace(description.ToString()))
                {
                    builder.AddField(x =>
                    {
                        x.Name = module.Name;
                        x.Value = description.ToString();
                        x.IsInline = false;
                    });
                }
            }

            try
            {
                var DMChannel = await context.User.GetOrCreateDMChannelAsync();
                await DMChannel.SendMessageAsync("", false, builder.Build());
            }
            catch (HttpException)
            {
                await context.Channel.SendMessageAsync(
                    "Error: You have PMs disabled on this server. Please enable direct messages in your privacy settings and try again.");
            }
        }

        public async Task HelpAsync(SocketCommandContext context, string command)
        {
            var result = _service.Search(context, command);

            if (!result.IsSuccess)
            {
                await context.Channel.SendMessageAsync($"Sorry, I couldn't find a command like **{command}**.");
                return;
            }
            var builder = new EmbedBuilder
            {
                Color = new Color(114, 137, 218),
                Description = $"Here are some commands like **{command}**"

            };

            foreach (var match in result.Commands)
            {
                var cmd = match.Command;
                var param = cmd.Parameters.Select(p => p.Name);
                builder.AddField(x =>
                {
                    x.Name = string.Join(", ", cmd.Aliases);
                    x.Value = $"Parameters: {(param.Any() ? string.Join(", ", param) : "None")}\n" +
                              $"Summary: {cmd.Summary}";
                    x.IsInline = false;
                });
            }

            await context.Channel.SendMessageAsync("", false, builder.Build());
        }
    }
}
