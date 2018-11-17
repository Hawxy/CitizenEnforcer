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
using CitizenEnforcer.TypeReaders;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace CitizenEnforcer.Services
{
    public class CommandHandler
    {
        private readonly IServiceProvider _provider;
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commandService;
        private readonly IConfiguration _config;
        private readonly Helper _helper;

        public CommandHandler(IServiceProvider provider, DiscordSocketClient client, CommandService commandService, IConfiguration config, Helper helper)
        {
            _provider = provider;
            _client = client;
            _commandService = commandService;
            _config = config;
            _helper = helper;
            _client.MessageReceived += HandleMessageAsync;
            _commandService.CommandExecuted += CommandExecutedAsync;
        }

        public async Task InitializeAsync()
        {
            _commandService.AddTypeReader<IBan>(new IBanTypeReader());
            await _commandService.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);
        }

        private async Task HandleMessageAsync(SocketMessage incomingMessage)
        {
            if (!(incomingMessage is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;

            int argPos = 0;
            if (!message.HasStringPrefix(_config["Prefix"], ref argPos) && !message.HasMentionPrefix(_client.CurrentUser, ref argPos)) return;

            var context = new SocketCommandContext(_client, message);
            await _commandService.ExecuteAsync(context, argPos, _provider);
        }
        public async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            if (!command.IsSpecified && result.IsSuccess)
                return;

            if (result.Error.HasValue)
            {
                switch (result.Error)
                {
                    case CommandError.UnknownCommand:
                        break;
                    case CommandError.BadArgCount:
                        await context.Channel.SendMessageAsync("Incorrect command usage, showing helper:");
                        EmbedBuilder builder = _helper.GetHelpInformationBuilder(command.Value);
                        await context.Channel.SendMessageAsync(embed: builder.Build());
                        break;
                    case CommandError.ParseFailed:
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