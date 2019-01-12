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
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CitizenEnforcer.Context;
using CitizenEnforcer.InteractiveCriterion;
using CitizenEnforcer.Models;
using CitizenEnforcer.Preconditions;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
// ReSharper disable SimplifyLinqExpression

namespace CitizenEnforcer.Modules
{
    [Group("setup")]
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(GuildPermission.KickMembers)]
    public class SetupModule : ModuleBase<SocketCommandContext>
    {
        public InteractiveService _interactive { get; set; }
        public BotContext _botContext { get; set; }

        //I could probably add some more error handling here, but I don't expect the bot to be used outside of a single server
        [Command("initialize")]
        public async Task Initialize()
        {
            #region Setting up guild text log channel

            if (await _botContext.Guilds.AnyAsync(x => x.GuildId == Context.Guild.Id))
            {
                await ReplyAsync("This guild is already registered, use the set commands to modify settings!");
            }
            await ReplyAsync("State the logging channel that guild events will be saved to (message edit/deletes + user edits):");
            var response = await _interactive.NextMessageAsync(Context, new MentionsChannel(), TimeSpan.FromSeconds(200));
            var channel = response.MentionedChannels.ElementAt(0);

            var guild = new Guild
            {
                GuildId = Context.Guild.Id,
                LoggingChannel = channel.Id
            };
            await _botContext.Guilds.AddAsync(guild);
            await _botContext.SaveChangesAsync();
            await ReplyAsync($"Message log channel set to {(channel as ITextChannel)?.Mention}");
            #endregion

            #region Channels to log
            await ReplyAsync("Mention all channels that the bot should log:");
            response = await _interactive.NextMessageAsync(Context, new MentionsChannel(), TimeSpan.FromSeconds(200));
            foreach (var cn in response.MentionedChannels)
            {
                await _botContext.RegisteredChannels.AddAsync(new RegisteredChannel
                {
                    ChannelId = cn.Id,
                    GuildId = Context.Guild.Id
                });
            }
            guild.IsEditLoggingEnabled = true;
            await _botContext.SaveChangesAsync();
            await ReplyAsync("Channels registered!");

            #endregion

            #region ModLog

            await ReplyAsync("Mention the channel you want moderation logs to be stored in:");
            response = await _interactive.NextMessageAsync(Context, new MentionsChannel(), TimeSpan.FromSeconds(200));
            var modchannel = response.MentionedChannels.ElementAt(0);

            guild.ModerationChannel = modchannel.Id;
            guild.IsModerationEnabled = true;
            await _botContext.SaveChangesAsync();
            await ReplyAsync($"Moderation channel set to {(modchannel as ITextChannel)?.Mention}");

            #endregion

            #region PublicAnnounce

            await ReplyAsync("Mention the channel you want public moderation messages to be sent to:");
            response = await _interactive.NextMessageAsync(Context, new MentionsChannel(), TimeSpan.FromSeconds(200));
            var publicchannel = response.MentionedChannels.ElementAt(0);
            guild.PublicAnnouceChannel = publicchannel.Id;
            guild.IsPublicAnnounceEnabled = true;
            await _botContext.SaveChangesAsync();
            await ReplyAsync($"Announce channel set to {(publicchannel as ITextChannel)?.Mention}, Setup complete!");
            #endregion
        }

        [Command]
        [RequireInitialized(InitializedType.Basic)]
        [Summary("Allows you to set LoggingChannel, ModerationChannel and PublicAnnounceChannel")]
        public async Task ChangeChannel(string changeField, ITextChannel channel)
        {
            var guild = await _botContext.Guilds.FirstOrDefaultAsync(x => x.GuildId == Context.Guild.Id);
            var field = typeof(Guild).GetProperty(changeField, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (field == null)
            {
                await ReplyAsync("Unable to find field");
                return;
            }

            field.SetValue(guild, channel.Id);
            await ReplyAsync($"{field.Name} set to {channel.Mention}");
            await _botContext.SaveChangesAsync();
        }

        [Command]
        [RequireInitialized(InitializedType.Basic)]
        [Summary("Allows you to set IsEditLoggingEnabled, IsModerationEnabled and IsPublicAnnounceEnabled")]
        public async Task ChangeBool(string changeField, bool modify)
        {
            var guild = await _botContext.Guilds.FirstOrDefaultAsync(x => x.GuildId == Context.Guild.Id);
            var field = typeof(Guild).GetProperty(changeField, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (field == null || field.PropertyType != typeof(bool))
            {
                await ReplyAsync("Unable to find field");
                return;
            }

            field.SetValue(guild, modify);
            await ReplyAsync($"{field.Name} set to {modify}");
            await _botContext.SaveChangesAsync();
           
        }

        [Group("registeredchannels")]
        [RequireInitialized(InitializedType.Basic)]
        public class RegisteredChannels : ModuleBase<SocketCommandContext>
        {
            public BotContext _botContext { get; set; }

            [Command]
            [Summary("Lists all currently registered channels")]
            public async Task RegisteredChannelsList()
            {
                var currentchannels = await _botContext.RegisteredChannels.AsNoTracking().Include(x => x.Guild).Where(x => x.GuildId == Context.Guild.Id).ToListAsync();
                StringBuilder builder = new StringBuilder("Currently tracked channels:\n");

                foreach (var registeredChannel in currentchannels)
                {
                    var channel = Context.Guild.GetChannel(registeredChannel.ChannelId);
                    builder.AppendJoin(", ", (channel as ITextChannel)?.Mention);
                }

                await ReplyAsync(builder.ToString());
            }
            //TODO rewrite this
            [Command("add")]
            [Summary("Adds additional channels to monitor for edit/deletes")]
            public async Task AddRegisteredChannels(params IGuildChannel[] channels)
            {
                var currentchannels = await _botContext.RegisteredChannels.Include(x => x.Guild).Where(x => x.GuildId == Context.Guild.Id).ToListAsync();
                foreach (IGuildChannel guildChannel in channels)
                {
                    if (!currentchannels.Any(x => x.ChannelId == guildChannel.Id))
                        _botContext.RegisteredChannels.Add(new RegisteredChannel(guildChannel.Id, Context.Guild.Id));
                }
                await _botContext.SaveChangesAsync();
                await ReplyAsync("<:thumbsup:338616449826291714>");
            }

            [Command("remove")]
            [Summary("Removes channels from monitoring list")]
            public async Task RemoveLoggingChannels(params IGuildChannel[] channels)
            {
                var currentchannels = await _botContext.RegisteredChannels.Include(x => x.Guild).Where(x => x.GuildId == Context.Guild.Id).ToListAsync();
                foreach (var registeredChannel in currentchannels)
                {
                    if (channels.Any(x => x.Id == registeredChannel.ChannelId))
                        _botContext.Remove(registeredChannel);
                }
                await _botContext.SaveChangesAsync();
                await ReplyAsync("<:thumbsup:338616449826291714>");
            }
        }
    }
}