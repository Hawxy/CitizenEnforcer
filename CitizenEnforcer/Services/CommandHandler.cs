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
using System.Reflection;
using System.Threading.Tasks;
using CitizenEnforcer.Settings;
using CitizenEnforcer.TypeReaders;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace CitizenEnforcer.Services
{
    public class CommandHandler
    {
        private readonly IServiceProvider _provider;
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commandService;
        private readonly Configuration _config;
        private readonly Helper _helper;

        public CommandHandler(IServiceProvider provider, DiscordSocketClient client, CommandService commandService, Configuration config, Helper helper)
        {
            _provider = provider;
            _client = client;
            _commandService = commandService;
            _config = config;
            _helper = helper;
            client.MessageReceived += HandleMessage;
        }

        public async Task InitializeAsync()
        {
            _commandService.AddTypeReader<IBan>(new IBanTypeReader());
            await _commandService.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);
        }

        private async Task HandleMessage(SocketMessage incmsg)
        {
            if (!(incmsg is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;

            int argPos = 0;
            //TODO prefix
            if (!message.HasStringPrefix(_config.Prefix, ref argPos) && !message.HasMentionPrefix(_client.CurrentUser, ref argPos)) return;

            var context = new SocketCommandContext(_client, message);
            var result = await _commandService.ExecuteAsync(context, argPos, _provider);
            if (result.Error.HasValue)
            {
                switch (result.Error)
                {
                    case CommandError.UnknownCommand:
                        break;
                    case CommandError.ParseFailed:
                        await context.Channel.SendMessageAsync(result.ErrorReason);
                        break;
                    case CommandError.BadArgCount:
                        string[] messagecontent = message.ToString().Split();
                        await context.Channel.SendMessageAsync("Incorrect command usage, showing helper:");
                        await _helper.HelpAsync(context, messagecontent[0].Replace(_config.Prefix, string.Empty));
                        break;
                    case CommandError.ObjectNotFound:
                    case CommandError.MultipleMatches:
                    case CommandError.UnmetPrecondition:
                        await context.Channel.SendMessageAsync(result.ErrorReason);
                        break;
                    case CommandError.Exception:
                        await context.Channel.SendMessageAsync("Unable to comply, an internal exception was detected.");
                        break;
                    case CommandError.Unsuccessful:
                        break;
                }
            }
        }
    }
}