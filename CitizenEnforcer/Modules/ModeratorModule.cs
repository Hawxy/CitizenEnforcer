using System;
using System.Linq;
using System.Threading.Tasks;
using CitizenEnforcer.Context;
using CitizenEnforcer.Preconditions;
using CitizenEnforcer.Services;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;

namespace CitizenEnforcer.Modules
{
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(GuildPermission.BanMembers)]
    [RequireInitializedAccessible(InitializedType.All)]
    public class ModeratorModule : ModuleBase<SocketCommandContext>
    {
        public BotContext _botContext { get; set; }
        public InteractiveService _interactiveService { get; set; }
        public ModerationService _moderationService { get; set; }

        [Command("warn")]
        [Alias("w")]
        [Summary("Logs a user warning. Supports an optional specified reason")]
        public async Task Warn(IGuildUser user, [Remainder] string reason = null) => await _moderationService.WarnUser(Context, user, reason);

        [Command("kick")]
        [Alias("k")]
        [RequireBotPermission(GuildPermission.KickMembers)]
        [Summary("Removes a user from the server. Supports an optional specified reason")]
        public async Task Kick(IGuildUser user, [Remainder] string reason = null) => await _moderationService.KickUser(Context, user, reason);

        [Command("tempban")]
        [Alias("tb")]
        [RequireBotPermission(GuildPermission.BanMembers)]
        [Summary("Temporarily bans a user for 3 days. Supports an optional specified reason")]
        public async Task TempBan(IGuildUser user, [Remainder] string reason = null) => await _moderationService.TempBanUser(Context, user, reason);

        [Command("softban")]
        [Alias("sb")]
        [RequireBotPermission(GuildPermission.BanMembers)]
        [Summary("Bans a user without deleting message history. Supports an optional specified reason")]
        public async Task SoftBan(IGuildUser user, [Remainder] string reason = null) => await _moderationService.BanUser(Context, user, reason, false, false);

        [Priority(0)]
        [Command("ban")]
        [Alias("b")]
        [RequireBotPermission(GuildPermission.BanMembers)]
        [Summary("Bans and deletes recent message history. Supports an optional specified reason")]
        public async Task Ban(IGuildUser user, [Remainder] string reason = null) => await _moderationService.BanUser(Context, user, reason, false, true);

        [Priority(1)]
        [Command("ban")]
        [Alias("b")]
        [RequireBotPermission(GuildPermission.BanMembers)]
        [Summary("Increases an existing temp-ban to a permanent ban. Supports an optional specified reason")]
        public async Task Ban(IBan bannedUser, [Remainder] string reason = null) => await _moderationService.BanUser(Context, bannedUser.User, reason, true, false);

        [Command("unban")]
        [Alias("ub")]
        [RequireBotPermission(GuildPermission.BanMembers)]
        [Summary("Unbans a given user")]
        public async Task Unban(IBan bannedUser) => await _moderationService.UnbanUser(Context, bannedUser.User);
        
        [Command("reason")]
        [Alias("r")]
        [Summary("Sets the reason of a given caseID")]
        public async Task Reason(ulong caseID, [Remainder] string reason)
        {
            var entry = await _botContext.ModLogs.Include(z=> z.Guild).FirstOrDefaultAsync(x => x.ModLogCaseID == caseID && x.GuildId == Context.Guild.Id && x.ModId == Context.User.Id);
            if (entry == null)
            {
                await ReplyAsync("Either you are not the responsible moderator for this entry or the entry does not exist");
                return;
            }

            using (Context.Channel.EnterTypingState())
            {
                //modify the message
                var modChannel = Context.Guild.GetTextChannel(entry.Guild.ModerationChannel);
                var message = await modChannel.GetMessageAsync(entry.LoggedMessageId);
                if (message is IUserMessage userMessage)
                {
                    var embed = userMessage.Embeds.ElementAt(0).ToEmbedBuilder();
                    embed.Fields.First(y => y.Name == "Reason").Value = reason;
                    await userMessage.ModifyAsync(x => x.Embed = embed.Build());
                }
                //update the db entry
                entry.Reason = reason;
                await _botContext.SaveChangesAsync();

                //lets be nice and cleanup afterwards
                await Context.Message.DeleteAsync();
                await _interactiveService.ReplyAndDeleteAsync(Context, Emote.Parse("<:thumbsup:338616449826291714>").ToString(), timeout: TimeSpan.FromSeconds(5));
            }
        }
    }
}