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
using System.Threading.Tasks;
using CitizenEnforcer.Common;
using CitizenEnforcer.Context;
using CitizenEnforcer.Models;
using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;

namespace CitizenEnforcer.Services
{
    public class LookupService
    {
        private readonly BotContext _botContext;

        public LookupService(BotContext botContext)
        {
            _botContext = botContext;
        }
        //Use information from db if unable to resolve user.
        //TODO refactor this at some point
        public async Task LookupUser(SocketCommandContext context, ulong userID)
        {
            var foundLogs = await _botContext.ModLogs.AsNoTracking().Include(z => z.TempBan).Where(x => x.UserId == userID && x.GuildId == context.Guild.Id).ToListAsync();
            if (foundLogs.Count == 0)
            {
                await context.Channel.SendMessageAsync("No previous moderator actions have been found for this user, nor are they a member of this guild. Are you sure the ID is correct?");
                return;
            }
            var highestInfraction = foundLogs.Max(x => x.InfractionType);

            var builder = ModeratorFormats.GetUserLookupBuilder(foundLogs.Last().UserName, userID, foundLogs, highestInfraction, false);

            await context.Channel.SendMessageAsync("**WARN:** Unable to resolve user, falling back to last known information",embed: builder.Build());
        }

        public async Task LookupUser(SocketCommandContext context, IUser user)
        {
            var foundLogs = await _botContext.ModLogs.AsNoTracking().Include(z=> z.TempBan).Where(x => x.UserId == user.Id && x.GuildId == context.Guild.Id).ToListAsync();
            bool currentlyBanned = await context.Guild.GetBanSafelyAsync(user.Id) != null;
            
            if (foundLogs.Count == 0)
            {
                await context.Channel.SendMessageAsync($"No previous moderator actions have been found for this user. Currently banned: {currentlyBanned}");
                return;
            }
            var highestInfraction = foundLogs.Max(x => x.InfractionType);

            var builder = ModeratorFormats.GetUserLookupBuilder(user.ToString(), user.Id, foundLogs, highestInfraction, currentlyBanned);

            await context.Channel.SendMessageAsync(embed: builder.Build());
        }

        public async Task LookupCase(SocketCommandContext context, ulong caseID)
        {
            var foundCase = await _botContext.ModLogs.AsNoTracking().Include(y=> y.TempBan).FirstOrDefaultAsync(x => x.ModLogCaseID == caseID && x.GuildId == context.Guild.Id);
            if (foundCase == null)
            {
                await context.Channel.SendMessageAsync("Case not found");
                return;
            }

            //TODO maybe add a fallback for this at some point?
            var modUser = context.Client.GetUser(foundCase.ModId);

            //We prefer to get the latest username, but if we can't, fallback to the database.
            IUser caseUser = context.Client.GetUser(foundCase.UserId) ?? (await context.Guild.GetBanSafelyAsync(foundCase.UserId))?.User;
            string username = caseUser?.ToString() ?? foundCase.UserName;

            EmbedBuilder embed;
            switch (foundCase.InfractionType)
            {
                case InfractionType.Warning:
                    embed = ModeratorFormats.GetWarnBuilder(username, foundCase.UserId, modUser, caseID, foundCase.Reason, foundCase.DateTime);
                    break;
                case InfractionType.Kick:
                    embed = ModeratorFormats.GetKickBuilder(username, foundCase.UserId, modUser, caseID, foundCase.Reason, foundCase.DateTime);
                    break;
                case InfractionType.TempBan:
                    embed = ModeratorFormats.GetTempBanBuilder(username, foundCase.UserId, modUser, caseID, foundCase.Reason, foundCase.DateTime, foundCase.TempBan.ExpireDate);
                    break;
                case InfractionType.Ban:
                    embed = ModeratorFormats.GetBanBuilder(username, foundCase.UserId, modUser, caseID, foundCase.Reason, foundCase.DateTime);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            await context.Channel.SendMessageAsync(embed: embed.Build());
        }
    }
}
