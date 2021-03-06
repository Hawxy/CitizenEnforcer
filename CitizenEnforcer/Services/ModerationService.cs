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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenEnforcer.Common;
using CitizenEnforcer.Context;
using CitizenEnforcer.Models;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;
using EFCoreSecondLevelCacheInterceptor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Sentry;
using Serilog;

namespace CitizenEnforcer.Services
{
    public class ModerationService
    {
        private readonly IDbContextFactory<BotContext> _contextFactory;
        private readonly IMemoryCache _banCache;
        public ModerationService(IDbContextFactory<BotContext> contextFactory, DiscordSocketClient client, IMemoryCache memoryCache)
        {
            _contextFactory = contextFactory;
            _banCache = memoryCache;
            client.UserBanned += HandleUserBanned;
            client.UserUnbanned += HandleUserUnbanned;
        }

        private async Task HandleUserBanned(SocketUser bannedUser, SocketGuild guild)
        {
            await using var botContext = _contextFactory.CreateDbContext();
            //if the user is already cached then reject the ban event
            if (_banCache.TryGetValue(bannedUser.Id, out CacheModel value) && value.CacheType == CacheType.BanReject)
                return;

            _banCache.Set(bannedUser.Id, new CacheModel(guild.Id), TimeSpan.FromSeconds(5));

            if (!await botContext.Guilds.Cacheable().AsQueryable().AnyAsync(x => x.GuildId == guild.Id && x.IsModerationEnabled))
                return;

            var caseID = await GenerateNewCaseID(botContext, guild.Id);

            IEnumerable<RestAuditLogEntry> logs = null;
            try
            {
                logs = await guild.GetAuditLogsAsync(5).FlattenAsync();
            }
            catch (Exception e)
            {
                SentrySdk.CaptureException(e);
            }

            var entry = logs?.FirstOrDefault(x => (x.Data as BanAuditLogData)?.Target.Id == bannedUser.Id);

            //if entry isn't null then use the data contained
            var logEntry = entry != null
                ? new ModLog(entry.User, guild.Id, bannedUser, caseID, InfractionType.Ban, entry.Reason)
                : new ModLog(caseID, guild.Id, bannedUser, InfractionType.Ban);

            await botContext.ModLogs.AddAsync(logEntry);
            await botContext.SaveChangesAsync();

            var builder = ModeratorFormats.GetBanBuilder(bannedUser, entry?.User, caseID, entry?.Reason, logEntry.DateTime);
            var message = await SendEmbedToModLog(botContext, guild, builder);

            logEntry.LoggedMessageId = message.Id;
            await botContext.SaveChangesAsync();
            await SendMessageToAnnounce(botContext, guild, $"***{bannedUser} has been permanently banned from the server***");
        }
        private async Task HandleUserUnbanned(SocketUser bannedUser, SocketGuild guild)
        {
            await using var botContext = _contextFactory.CreateDbContext();
            //if the user is cached then reject the unban event
            if (_banCache.TryGetValue(bannedUser.Id, out CacheModel value) && value.CacheType == CacheType.UnbanReject)
                return;

            if (!await botContext.Guilds.Cacheable().AsQueryable().AnyAsync(x => x.GuildId == guild.Id && x.IsModerationEnabled))
                return;
            
            var foundtb = await botContext.TempBans.Include(x => x.ModLog).Cacheable().AsQueryable().FirstOrDefaultAsync(x => x.ModLog.UserId == bannedUser.Id && x.TempBanActive);
            if (foundtb != null)
            {
                foundtb.TempBanActive = false;
                await botContext.SaveChangesAsync();
            }

            IEnumerable<RestAuditLogEntry> logs = null;
            try
            {
                logs = await guild.GetAuditLogsAsync(5).FlattenAsync();
            }
            catch (Exception e)
            {
                SentrySdk.CaptureException(e);
            }

            var entry = logs?.FirstOrDefault(x => (x.Data as UnbanAuditLogData)?.Target.Id == bannedUser.Id);
            var builder = ModeratorFormats.GetUnbanBuilder(bannedUser, "Manual Unban", entry?.User);
            await SendEmbedToModLog(botContext,guild, builder);
            await SendMessageToAnnounce(botContext, guild, $"***{bannedUser}'s ban has been lifted***");

        }

        public async Task WarnUser(SocketCommandContext context, IGuildUser user, string reason)
        {
            await using var botContext = _contextFactory.CreateDbContext();
            await context.Message.DeleteAsync();
            ulong caseID = await GenerateNewCaseID(botContext, context.Guild.Id);
            var logEntry = new ModLog(context, user, caseID, InfractionType.Warning, reason);

            await botContext.ModLogs.AddAsync(logEntry);
            await botContext.SaveChangesAsync();

            var builder = ModeratorFormats.GetWarnBuilder(user, context.User, caseID, reason, logEntry.DateTime);

            var message = await SendEmbedToModLog(botContext, context.Guild, builder);
            logEntry.LoggedMessageId = message.Id;
            await botContext.SaveChangesAsync();

            await SendMessageToAnnounce(botContext, context.Guild, $"***{user} has received an official warning***");
            await SendMessageToUser(user, $"You have been warned on the guild ``{context.Guild.Name}`` {(string.IsNullOrWhiteSpace(reason) ? string.Empty : $"for: ``{reason}``" )}");
        }
        public async Task KickUser(SocketCommandContext context, IGuildUser user, string reason)
        {
            await using var botContext = _contextFactory.CreateDbContext();
            await context.Message.DeleteAsync();
            ulong caseID = await GenerateNewCaseID(botContext, context.Guild.Id);
            var logEntry = new ModLog(context, user, caseID, InfractionType.Kick, reason);

            await botContext.ModLogs.AddAsync(logEntry);
            await botContext.SaveChangesAsync();

            var builder = ModeratorFormats.GetKickBuilder(user, context.User, caseID, reason, logEntry.DateTime);

            await SendMessageToUser(user, $"You have been kicked from the guild ``{context.Guild.Name}`` {(string.IsNullOrWhiteSpace(reason) ? string.Empty : $"for: ``{reason}``" )}");

            //Kick
            await user.KickAsync();

            var message = await SendEmbedToModLog(botContext, context.Guild, builder);
            logEntry.LoggedMessageId = message.Id;
            await botContext.SaveChangesAsync();

            await SendMessageToAnnounce(botContext, context.Guild, $"***{user} has been kicked from the server***");
        }
        public async Task TempBanUser(SocketCommandContext context, IUser user, string reason)
        {
            await using var botContext = _contextFactory.CreateDbContext();
            await context.Message.DeleteAsync();
            ulong caseID = await GenerateNewCaseID(botContext, context.Guild.Id);
            var logEntry = new ModLog(context, user, caseID, InfractionType.TempBan, reason);

            await botContext.ModLogs.AddAsync(logEntry);
            await botContext.SaveChangesAsync();

            var tempBan = new TempBan
            {
                ModLog = logEntry,
                TempBanActive = true,
                ExpireDate = DateTimeOffset.UtcNow.AddDays(3)
            };
            _banCache.Set(user.Id, new CacheModel(context.Guild.Id), TimeSpan.FromSeconds(5));
            await botContext.TempBans.AddAsync(tempBan);

            var builder = ModeratorFormats.GetTempBanBuilder(user, context.User, caseID, reason, logEntry.DateTime, tempBan.ExpireDate);
            await SendMessageToUser(user, $"You have been temporarily banned from the guild ``{context.Guild.Name}`` {(string.IsNullOrWhiteSpace(reason) ? string.Empty : $"for: ``{reason}``")}\nThis ban will expire on ``{tempBan.ExpireDate.DateTime} UTC``");
            //Ban
            await context.Guild.AddBanAsync(user);

            var message = await SendEmbedToModLog(botContext, context.Guild, builder);
            logEntry.LoggedMessageId = message.Id;
            await botContext.SaveChangesAsync();

            await SendMessageToAnnounce(botContext, context.Guild, $"***{user} has been temporarily banned from the server***");
        }

        //TODO this handles too many different concerns. Clean it up at some point.
        public async Task BanUser(SocketCommandContext context, IUser user, BanType type, string reason)
        {
            await using var botContext = _contextFactory.CreateDbContext();
            await context.Message.DeleteAsync();

            var foundtb = await botContext.TempBans
                .Include(y => y.ModLog)
                .FirstOrDefaultAsync(x => x.TempBanActive && x.ModLog.UserId == user.Id && x.ModLog.GuildId == context.Guild.Id);
            //If the user is TB'd then they're already banned and we don't need to do anything.
            if (foundtb != null)
            {
                Log.Debug("Ban short circuit: A previous temp-ban was found for user {username}-{id}", user.Username, user.Id);
                foundtb.TempBanActive = false;
                await botContext.SaveChangesAsync();
            }
            else
            {
                //Ensure we don't double ban the user.
                var alreadyBanned = await context.Guild.GetBanSafelyAsync(user.Id);
                if (alreadyBanned != null)
                {
                    await context.Channel.SendMessageAsync("User already banned!");
                    return;
                }

                _banCache.Set(user.Id, new CacheModel(context.Guild.Id), TimeSpan.FromSeconds(5));
                await SendMessageToUser(user, $"You have been permanently banned from the guild ``{context.Guild.Name}`` {(string.IsNullOrWhiteSpace(reason) ? string.Empty : $"for: ``{reason}``")}");
                if (type == BanType.HardBan)
                    await context.Guild.AddBanAsync(user, 2);
                else
                    await context.Guild.AddBanAsync(user);
            }

            ulong caseID = await GenerateNewCaseID(botContext, context.Guild.Id);
            var logEntry = new ModLog(context, user, caseID, InfractionType.Ban, reason);
            
            await botContext.ModLogs.AddAsync(logEntry);
            await botContext.SaveChangesAsync();

            var builder = ModeratorFormats.GetBanBuilder(user, context.User, caseID, reason, logEntry.DateTime);

            var message = await SendEmbedToModLog(botContext, context.Guild, builder);
            logEntry.LoggedMessageId = message.Id;
            await botContext.SaveChangesAsync();

            await SendMessageToAnnounce(botContext, context.Guild, $"***{user} has been permanently banned from the server***");
        }

        public async Task UnbanUser(SocketCommandContext context, IUser bannedUser)
        {
            await using var botContext = _contextFactory.CreateDbContext();
            await context.Message.DeleteAsync();
            var foundtb = await botContext.TempBans.Include(x => x.ModLog).Cacheable().AsQueryable().FirstOrDefaultAsync(x => x.ModLog.UserId == bannedUser.Id && x.TempBanActive);
            if (foundtb != null)
            {
                foundtb.TempBanActive = false;
                await botContext.SaveChangesAsync();
            }
            _banCache.Set(bannedUser.Id, new CacheModel(context.Guild.Id, CacheType.UnbanReject), TimeSpan.FromSeconds(5));
            await context.Guild.RemoveBanAsync(bannedUser);
            var builder = ModeratorFormats.GetUnbanBuilder(bannedUser, "Manual Unban", context.User);
            await SendEmbedToModLog(botContext, context.Guild, builder);
            await SendMessageToAnnounce(botContext, context.Guild, $"***{bannedUser}'s ban has been lifted***");
        }

        public async Task<IUserMessage> SendEmbedToModLog(BotContext context, SocketGuild guild, EmbedBuilder embed)
        {
            var foundGuild = await context.Guilds.AsNoTracking().FirstOrDefaultAsync(x => x.GuildId == guild.Id);
            var channel = guild.GetTextChannel(foundGuild.ModerationChannel);
            return await channel.SendMessageAsync("", embed: embed.Build());
        }

        public async Task SendMessageToAnnounce(BotContext context, SocketGuild guild, string message = "", EmbedBuilder embed = null)
        {
            var foundGuild = await context.Guilds.AsNoTracking().FirstOrDefaultAsync(x => x.GuildId == guild.Id && x.IsPublicAnnounceEnabled);
            //public announce disabled
            if (foundGuild == null)
                return;

            var channel = guild.GetTextChannel(foundGuild.PublicAnnouceChannel);

            await channel.SendMessageAsync(message, embed: embed?.Build());
        }

        public async Task SendMessageToUser(IUser user, string message)
        {
            try
            {
                var DMChannel = await user.GetOrCreateDMChannelAsync();
                await DMChannel.SendMessageAsync(message);
            }
            //Can't really do much here. Logging the error would get too spammy in a moderation context.
            catch (HttpException){}
        }

        private async Task<ulong> GenerateNewCaseID(BotContext context, ulong GuildID)
        {
            var logEntries = await context.ModLogs.AsNoTracking().AsAsyncEnumerable().LastOrDefaultAsync(x => x.GuildId == GuildID);
            return logEntries?.ModLogCaseID + 1 ?? 1;
        }

        public class CacheModel
        {
            public CacheModel(ulong guildID, CacheType cacheType = CacheType.BanReject)
            {
                GuildID = guildID;
                CacheType = cacheType;
            }
            public ulong GuildID { get; set; }
            public CacheType CacheType { get; set; }
        }
        public enum CacheType
        {
            BanReject,
            UnbanReject
        }

        public enum BanType
        {
            HardBan,
            SoftBan
        }
    }
}
