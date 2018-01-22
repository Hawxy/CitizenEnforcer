using System;
using System.Linq;
using System.Threading.Tasks;
using CitizenEnforcer.Settings;
using Discord;
using Discord.Commands;
using Discord.Net;

namespace CitizenEnforcer.Services
{
    public class Helper
    {
        private readonly CommandService _service;
        private readonly IServiceProvider _map;
        private readonly Configuration _config;

        public Helper(CommandService service, IServiceProvider map, Configuration config)
        {
            _service = service;
            _map = map;
            _config = config;
        }

        public async Task HelpAsync(SocketCommandContext context)
        {
            string prefix = _config.Prefix;
            var builder = new EmbedBuilder
            {
                Color = new Color(114, 137, 218),
                Description = $"These are the commands you have access to on the guild **{context.Guild.Name}**.\nAvailable commands differ based on your guild permissions.\nFor more information, use {_config.Prefix}help [command]"
            };

            foreach (var module in _service.Modules)
            {
                if(module.Name == "HelpModule")
                    continue;
                string description = null;
                foreach (var cmd in module.Commands)
                {
                    var result = await cmd.CheckPreconditionsAsync(context, _map);
                    if (result.IsSuccess)
                    {
                        description += $"{prefix}{cmd.Aliases.First()}\n";
                    }
                }
                if (!string.IsNullOrWhiteSpace(description))
                {
                    builder.AddField(x =>
                    {
                        x.Name = module.Name;
                        x.Value = description;
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
