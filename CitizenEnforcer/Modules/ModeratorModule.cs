﻿#region License
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
using System.Threading.Tasks;
using CitizenEnforcer.Common;
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
    [RequireInitialized(InitializedType.All)]
    public class ModeratorModule : ModuleBase<SocketCommandContext>
    {
        public BotContext _botContext { get; set; }
        public InteractiveService _interactiveService { get; set; }
        public ModerationService _moderationService { get; set; }

        [Command("warn")]
        [Alias("w")]
        [Summary("Logs a user warning. Supports an optional specified reason")]
        public Task Warn([NotSelf]IGuildUser user, [Remainder] string reason = null) => _moderationService.WarnUser(Context, user, Context.Message.ResolveAtString(reason));

        [Command("kick")]
        [Alias("k")]
        [RequireBotPermission(GuildPermission.KickMembers)]
        [Summary("Removes a user from the server. Supports an optional specified reason")]
        public Task Kick([NotSelf]IGuildUser user, [Remainder] string reason = null) => _moderationService.KickUser(Context, user, Context.Message.ResolveAtString(reason));

        [Command("tempban")]
        [Alias("tb")]
        [RequireBotPermission(GuildPermission.BanMembers)]
        [Summary("Temporarily bans a user for 3 days. Supports an optional specified reason")]
        public Task TempBan([NotSelf]IGuildUser user, [Remainder] string reason = null) => _moderationService.TempBanUser(Context, user, Context.Message.ResolveAtString(reason));

        [Command("softban")]
        [Alias("sb")]
        [RequireBotPermission(GuildPermission.BanMembers)]
        [Summary("Bans a user without deleting message history. Supports an optional specified reason")]
        public Task SoftBan([NotSelf]IGuildUser user, [Remainder] string reason = null) => _moderationService.BanUser(Context, user, Context.Message.ResolveAtString(reason), false, false);

        [Priority(1000)]
        [Command("ban")]
        [Alias("b")]
        [RequireBotPermission(GuildPermission.BanMembers)]
        [Summary("Bans and deletes recent message history. Supports an optional specified reason")]
        public Task Ban([NotSelf]IGuildUser user, [Remainder] string reason = null) => _moderationService.BanUser(Context, user, Context.Message.ResolveAtString(reason), false, true);

        [Priority(500)]
        [Command("ban")]
        [Alias("b")]
        [RequireBotPermission(GuildPermission.BanMembers)]
        [Summary("Increases an existing temp-ban to a permanent ban. Supports an optional specified reason")]
        public Task Ban(IBan bannedUser, [Remainder] string reason = null) => _moderationService.BanUser(Context, bannedUser.User, Context.Message.ResolveAtString(reason), true, false);

        [Command("unban")]
        [Alias("ub")]
        [RequireBotPermission(GuildPermission.BanMembers)]
        [Summary("Unbans a given user")]
        public Task Unban(IBan bannedUser) => _moderationService.UnbanUser(Context, bannedUser.User);
        
        [Command("reason")]
        [Alias("r")]
        [Summary("Sets the reason of a given caseID")]
        public async Task Reason(ulong caseID, [Remainder] string reason)
        {
            reason = Context.Message.ResolveAtString(reason);
            var entry = await _botContext.ModLogs.Include(z=> z.Guild).FirstOrDefaultAsync(x => x.ModLogCaseID == caseID && x.GuildId == Context.Guild.Id);
            //Is an event-driven entry
            if (entry?.ModId == 0)
            {
                using (Context.Channel.EnterTypingState())
                {
                    //modify the message
                    var modChannel = Context.Guild.GetTextChannel(entry.Guild.ModerationChannel);
                    var message = await modChannel.GetMessageAsync(entry.LoggedMessageId);
                    if (message is IUserMessage userMessage)
                    {
                        var embed = userMessage.Embeds.ElementAt(0).ToEmbedBuilder();
                        embed.Fields.First(y => y.Name == "Reason").Value = reason;
                        embed.WithAuthor(Context.User);
                        await userMessage.ModifyAsync(x => x.Embed = embed.Build());
                    }
                    //update the db entry
                    entry.Reason = reason;
                    entry.ModId = Context.User.Id;
                    entry.ModName = FormatUtilities.GetFullName(Context.User);
                    await _botContext.SaveChangesAsync();

                    //lets be nice and cleanup afterwards
                    await Context.Message.DeleteAsync();
                    await _interactiveService.ReplyAndDeleteAsync(Context, "<:thumbsup:338616449826291714>", timeout: TimeSpan.FromSeconds(5));
                }
            }
            else if (entry?.ModId == Context.User.Id)
            {
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
                    await _interactiveService.ReplyAndDeleteAsync(Context, "<:thumbsup:338616449826291714>", timeout: TimeSpan.FromSeconds(5));
                }
            }
            else
            {
                await ReplyAsync("You are not the responsible moderator for this entry, or the entry does not exist");
            }
        }
    }
}