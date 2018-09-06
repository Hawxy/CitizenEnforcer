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

        public async Task LookupUser(SocketCommandContext context, IUser user)
        {
            var foundLogs = await _botContext.ModLogs.Include(z=> z.TempBan).Where(x => x.UserId == user.Id && x.GuildId == context.Guild.Id).ToListAsync();
            bool currentlybanned = await context.Guild.GetBanSafelyAsync(user.Id) != null;
            
            if (foundLogs.Count == 0)
            {
                await context.Channel.SendMessageAsync($"No previous moderator actions have been found for this user. Currently banned: {currentlybanned}");
                return;
            }
            var highestInfraction = foundLogs.Max(x => x.InfractionType);

            var builder = ModeratorFormats.GetUserLookupBuilder(user, foundLogs, highestInfraction, currentlybanned);

            await context.Channel.SendMessageAsync("", embed: builder.Build());
        }

        public async Task LookupCase(SocketCommandContext context, ulong caseID)
        {
            var foundCase = await _botContext.ModLogs.FirstOrDefaultAsync(x => x.ModLogCaseID == caseID && x.GuildId == context.Guild.Id);
            if (foundCase == null)
            {
                await context.Channel.SendMessageAsync("Case not found");
                return;
            }

            var modUser = context.Guild.GetUser(foundCase.ModId);
            var caseUser = context.Guild.GetUser(foundCase.UserId);
            IUser banUser = null;
            if (caseUser == null)
            {
                banUser = (await context.Guild.GetBanSafelyAsync(foundCase.UserId))?.User;
            }

            EmbedBuilder embed;
            switch(foundCase.InfractionType)
                {
                    case InfractionType.Warning:
                        embed = caseUser != null
                            ? ModeratorFormats.GetWarnBuilder(caseUser, modUser, caseID, foundCase.Reason, foundCase.DateTime)
                            : ModeratorFormats.GetWarnBuilder(banUser, modUser, caseID, foundCase.Reason, foundCase.DateTime);
                        break;
                    case InfractionType.Kick:
                        embed = caseUser != null
                            ? ModeratorFormats.GetKickBuilder(caseUser, modUser, caseID, foundCase.Reason, foundCase.DateTime)
                            : ModeratorFormats.GetKickBuilder(banUser, modUser, caseID, foundCase.Reason, foundCase.DateTime);
                        break;
                    case InfractionType.TempBan:
                        embed = caseUser != null
                            ? ModeratorFormats.GetTempBanBuilder(caseUser, modUser, caseID, foundCase.Reason, foundCase.DateTime, foundCase.TempBan.ExpireDate)
                            : ModeratorFormats.GetTempBanBuilder(banUser, modUser, caseID, foundCase.Reason, foundCase.DateTime, foundCase.TempBan.ExpireDate);
                        break;
                    case InfractionType.Ban:
                        embed = caseUser != null
                            ? ModeratorFormats.GetBanBuilder(caseUser, modUser, caseID, foundCase.Reason, foundCase.DateTime)
                            : ModeratorFormats.GetBanBuilder(banUser, modUser, caseID, foundCase.Reason, foundCase.DateTime);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

            await context.Channel.SendMessageAsync("", embed: embed.Build());
        }
    }
}
