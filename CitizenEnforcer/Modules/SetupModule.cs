using System;
using System.Linq;
using System.Threading.Tasks;
using CitizenEnforcer.Context;
using CitizenEnforcer.InteractiveCriterion;
using CitizenEnforcer.Models;
using CitizenEnforcer.Preconditions;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;

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
            await ReplyAsync("State the logging channel that message events will be saved to (edit/deletes):");
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
            await ReplyAsync($"Announce channel set to {(publicchannel as ITextChannel)?.Mention}");

            #endregion
        }

        [Command]
        [RequireInitializedAccessible(InitializedType.Basic)]
        [Summary("Allows you to set LoggingChannel, ModerationChannel and PublicAnnounceChannel")]
        public async Task ChangeChannel(string changeField, ITextChannel channel)
        {
            var guild = await _botContext.Guilds.FirstOrDefaultAsync(x => x.GuildId == Context.Guild.Id);
            var fields = typeof(Guild).GetProperties();
            foreach (var field in fields)
            {
                if (changeField.Equals(field.Name, StringComparison.CurrentCultureIgnoreCase) && field.PropertyType == typeof(ulong) && field.Name != "GuildId")
                {
                    field.SetValue(guild, channel.Id);
                    await ReplyAsync($"{field.Name} set to {channel.Mention}");
                    await _botContext.SaveChangesAsync();
                    return;
                }
            }
            await ReplyAsync("Unable to find field");
        }

        [Command]
        [RequireInitializedAccessible(InitializedType.Basic)]
        [Summary("Allows you to set IsEditLoggingEnabled, IsModerationEnabled and IsPublicAnnounceEnabled")]
        public async Task ChangeBool(string changeField, bool modify)
        {
            var guild = await _botContext.Guilds.FirstOrDefaultAsync(x => x.GuildId == Context.Guild.Id);
            var fields = typeof(Guild).GetProperties();
            foreach (var field in fields)
            {
                if (changeField.Equals(field.Name, StringComparison.CurrentCultureIgnoreCase) && field.PropertyType == typeof(bool))
                {
                    field.SetValue(guild, modify);
                    await ReplyAsync($"{field.Name} set to {modify}");
                    await _botContext.SaveChangesAsync();
                    return;
                }
            }
            await ReplyAsync("Unable to find field");
        }
    }
}