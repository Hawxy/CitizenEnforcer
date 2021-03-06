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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CitizenEnforcer.TypeReaders;
using Discord;
using Discord.Addons.Hosting;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Sentry;
using Sentry.Protocol;

namespace CitizenEnforcer.Services
{
    public class CommandHandler : InitializedService
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

        public override async Task InitializeAsync(CancellationToken cancellationToken)
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

            using (SentrySdk.PushScope())
            {
                using var typing = incomingMessage.Channel.EnterTypingState();
                var context = new SocketCommandContext(_client, message);
                SentrySdk.ConfigureScope(s =>
                {
                    s.User = new User { Id = message.Author.Id.ToString(), Username = message.Author.ToString() };
                    s.SetTag("Guild", context.Guild?.Name ?? "Private Message");
                    s.SetTag("Channel", context.Channel?.Name ?? "N/A");
                });

                await _commandService.ExecuteAsync(context, argPos, _provider);
            }
           
        }
        public async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            if (!command.IsSpecified && result.IsSuccess)
                return;

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
                    await context.Channel.SendMessageAsync($"An error occurred whilst processing this command. This has been reported automatically. (ID: {SentrySdk.LastEventId})");
                    break;
                case CommandError.Unsuccessful:
                    break;
                case null:
                    break;
            }
        }
    }
}