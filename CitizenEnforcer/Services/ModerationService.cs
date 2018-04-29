using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenEnforcer.Common;
using CitizenEnforcer.Context;
using CitizenEnforcer.Models;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace CitizenEnforcer.Services
{
    public class ModerationService
    {
        private readonly BotContext _botContext;
        public List<Tuple<ulong, ulong>> BanCache { get; set; } = new List<Tuple<ulong, ulong>>();
        public ModerationService(BotContext botContext, DiscordSocketClient client)
        {
            _botContext = botContext;
            client.UserBanned += HandleUserBanned;
        }

        private async Task HandleUserBanned(SocketUser bannedUser, SocketGuild guild)
        {
            var foundBan = BanCache.FirstOrDefault(x => x.Item1 == guild.Id && x.Item2 == bannedUser.Id);
            if (foundBan != null)
            {
                BanCache.Remove(foundBan);
                return;
            }

            if (!await _botContext.Guilds.AnyAsync(x => x.GuildId == guild.Id && x.IsModerationEnabled))
                return;
            
            var caseID = await GenerateNewCaseID(guild.Id);
            var logEntry = new ModLog(caseID, guild.Id, bannedUser, InfractionType.Ban);

            await _botContext.ModLogs.AddAsync(logEntry);
            await _botContext.SaveChangesAsync();

            var builder = FormatUtilities.GetBanBuilder(bannedUser, null, caseID, null, logEntry.DateTime);
            var message = await SendMessageToModLog(guild, builder);

            logEntry.LoggedMessageId = message.Id;
            await _botContext.SaveChangesAsync();
        }


        public async Task WarnUser(SocketCommandContext context, IGuildUser user, string reason)
        {
            await context.Message.DeleteAsync();
            ulong caseID = await GenerateNewCaseID(context.Guild.Id);
            var logEntry = new ModLog(context, user, caseID, InfractionType.Warning, reason);

            await _botContext.ModLogs.AddAsync(logEntry);
            await _botContext.SaveChangesAsync();

            var builder = FormatUtilities.GetWarnBuilder(user, context.User, caseID, reason, logEntry.DateTime);

            var message = await SendMessageToModLog(context.Guild, builder);
            logEntry.LoggedMessageId = message.Id;
            await _botContext.SaveChangesAsync();

            await SendMessageToAnnounce(context.Guild, $"***{FormatUtilities.GetFullName(user)} has received an official warning***");
            await SendMessageToUser(user, $"You have been warned on the guild ``{context.Guild.Name}`` {(string.IsNullOrWhiteSpace(reason) ? string.Empty : $"for: ``{reason}``" )}");
        }
        public async Task KickUser(SocketCommandContext context, IGuildUser user, string reason)
        {
            await context.Message.DeleteAsync();
            ulong caseID = await GenerateNewCaseID(context.Guild.Id);
            var logEntry = new ModLog(context, user, caseID, InfractionType.Kick, reason);

            await _botContext.ModLogs.AddAsync(logEntry);
            await _botContext.SaveChangesAsync();

            var builder = FormatUtilities.GetKickBuilder(user, context.User, caseID, reason, logEntry.DateTime);

            await SendMessageToUser(user, $"You have been kicked from the guild ``{context.Guild.Name}`` {(string.IsNullOrWhiteSpace(reason) ? string.Empty : $"for: ``{reason}``" )}");

            //Kick
            await user.KickAsync();

            var message = await SendMessageToModLog(context.Guild, builder);
            logEntry.LoggedMessageId = message.Id;
            await _botContext.SaveChangesAsync();

            await SendMessageToAnnounce(context.Guild, $"***{FormatUtilities.GetFullName(user)} has been kicked from the server***");
        }
        public async Task TempBanUser(SocketCommandContext context, IUser user, string reason)
        {
            await context.Message.DeleteAsync();
            ulong caseID = await GenerateNewCaseID(context.Guild.Id);
            var logEntry = new ModLog(context, user, caseID, InfractionType.TempBan, reason);

            await _botContext.ModLogs.AddAsync(logEntry);
            await _botContext.SaveChangesAsync();

            var tempBan = new TempBan
            {
                ModLog = logEntry,
                TempBanActive = true,
                ExpireDate = DateTimeOffset.UtcNow.AddDays(3)
            };
            BanCache.Add(new Tuple<ulong, ulong>(context.Guild.Id, user.Id));
            await _botContext.TempBans.AddAsync(tempBan);

            var builder = FormatUtilities.GetTempBanBuilder(user, context.User, caseID, reason, logEntry.DateTime, tempBan.ExpireDate);
            await SendMessageToUser(user, $"You have been temporarily banned from the guild ``{context.Guild.Name}`` {(string.IsNullOrWhiteSpace(reason) ? string.Empty : $"for: ``{reason}``")}\nThis ban will expire on ``{tempBan.ExpireDate.DateTime} UTC``");
            //Ban
            await context.Guild.AddBanAsync(user);

            var message = await SendMessageToModLog(context.Guild, builder);
            logEntry.LoggedMessageId = message.Id;
            await _botContext.SaveChangesAsync();

            await SendMessageToAnnounce(context.Guild, $"***{FormatUtilities.GetFullName(user)} has been temporarily banned from the server***");
        }
        public async Task BanUser(SocketCommandContext context, IUser user, string reason, bool isEscalation, bool isHardBan)
        {
            await context.Message.DeleteAsync();
            if (isEscalation)
            {
                var foundtb = await _botContext.TempBans.Include(y => y.ModLog).FirstOrDefaultAsync(x => x.TempBanActive && x.ModLog.UserId == user.Id);
                if (foundtb != null)
                {
                    foundtb.TempBanActive = false;
                    await _botContext.SaveChangesAsync();
                }
                else
                {
                    await context.Channel.SendMessageAsync("No temp-ban on record to escalate");
                    return;
                }
            }
            else
            {
                BanCache.Add(new Tuple<ulong, ulong>(context.Guild.Id, user.Id));
                await SendMessageToUser(user, $"You have been permanently banned from the guild ``{context.Guild.Name}`` {(string.IsNullOrWhiteSpace(reason) ? string.Empty : $"for: ``{reason}``")}");
                if (isHardBan)
                    await context.Guild.AddBanAsync(user, 2);
                else
                    await context.Guild.AddBanAsync(user);
            }

            ulong caseID = await GenerateNewCaseID(context.Guild.Id);
            var logEntry = new ModLog(context, user, caseID, InfractionType.Ban, reason);
            
            await _botContext.ModLogs.AddAsync(logEntry);
            await _botContext.SaveChangesAsync();

            var builder = FormatUtilities.GetBanBuilder(user, context.User, caseID, reason, logEntry.DateTime);

            var message = await SendMessageToModLog(context.Guild, builder);
            logEntry.LoggedMessageId = message.Id;
            await _botContext.SaveChangesAsync();

            await SendMessageToAnnounce(context.Guild, $"***{FormatUtilities.GetFullName(user)} has been permanently banned from the server***");
        }

        public async Task UnbanUser(SocketCommandContext context, IUser bannedUser)
        {
            await context.Message.DeleteAsync();
            var foundtb = await _botContext.TempBans.Include(x => x.ModLog).FirstOrDefaultAsync(x => x.ModLog.UserId == bannedUser.Id && x.TempBanActive);
            if (foundtb != null)
            {
                foundtb.TempBanActive = false;
                await _botContext.SaveChangesAsync();
            }
            await context.Guild.RemoveBanAsync(bannedUser);
            var builder = FormatUtilities.GetUnbanBuilder(bannedUser, "Manual Unban", context.User);
            await SendMessageToModLog(context.Guild, builder);
            await SendMessageToAnnounce(context.Guild, $"***{FormatUtilities.GetFullName(bannedUser)}'s ban has been lifted***");
        }

        public async Task<IUserMessage> SendMessageToModLog(SocketGuild guild, EmbedBuilder embed)
        {
            var foundGuild = await _botContext.Guilds.AsNoTracking().FirstOrDefaultAsync(x => x.GuildId == guild.Id);
            var channel = guild.GetTextChannel(foundGuild.ModerationChannel);
            return await channel.SendMessageAsync("", embed: embed.Build());
        }

        public async Task SendMessageToAnnounce(SocketGuild guild, string message)
        {
            var foundGuild = await _botContext.Guilds.AsNoTracking().FirstOrDefaultAsync(x => x.GuildId == guild.Id && x.IsPublicAnnounceEnabled);
            //public announce disabled
            if (foundGuild == null)
                return;
            var channel = guild.GetTextChannel(foundGuild.PublicAnnouceChannel);
            await channel.SendMessageAsync(message);
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

        private async Task<ulong> GenerateNewCaseID(ulong GuildID)
        {
            var logEntries = await _botContext.ModLogs.AsNoTracking().LastOrDefaultAsync(x => x.GuildId == GuildID);
            return logEntries?.ModLogCaseID + 1 ?? 1;
        }
    }
}
