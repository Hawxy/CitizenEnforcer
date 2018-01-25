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
            bool currentlybanned = (await context.Guild.GetBansAsync()).Any(x=> x.User.Id == user.Id);
            if (foundLogs.Count == 0)
            {
                await context.Channel.SendMessageAsync($"No previous moderator actions have been found for this user. Currently banned: {currentlybanned}");
                return;
            }
            var highestInfraction = foundLogs.Max(x => x.InfractionType);
            var caseIDs = foundLogs.Select(x => x.ModLogCaseID).ToList();

            var builder = FormatUtilities.GetUserLookupBuilder(user, caseIDs, highestInfraction, currentlybanned);

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
                banUser = (await context.Guild.GetBansAsync()).FirstOrDefault(x => x.User.Id == foundCase.UserId)?.User;
            }

            EmbedBuilder embed;
            switch(foundCase.InfractionType)
                {
                    case InfractionType.Warning:
                        embed = caseUser != null
                            ? FormatUtilities.GetWarnBuilder(caseUser, modUser, caseID, foundCase.Reason, foundCase.DateTime)
                            : FormatUtilities.GetWarnBuilder(banUser, modUser, caseID, foundCase.Reason, foundCase.DateTime);
                        break;
                    case InfractionType.Kick:
                        embed = caseUser != null
                            ? FormatUtilities.GetKickBuilder(caseUser, modUser, caseID, foundCase.Reason, foundCase.DateTime)
                            : FormatUtilities.GetKickBuilder(banUser, modUser, caseID, foundCase.Reason, foundCase.DateTime);
                        break;
                    case InfractionType.TempBan:
                        embed = caseUser != null
                            ? FormatUtilities.GetTempBanBuilder(caseUser, modUser, caseID, foundCase.Reason, foundCase.DateTime, foundCase.TempBan.ExpireDate)
                            : FormatUtilities.GetTempBanBuilder(banUser, modUser, caseID, foundCase.Reason, foundCase.DateTime, foundCase.TempBan.ExpireDate);
                        break;
                    case InfractionType.Ban:
                        embed = caseUser != null
                            ? FormatUtilities.GetBanBuilder(caseUser, modUser, caseID, foundCase.Reason, foundCase.DateTime)
                            : FormatUtilities.GetBanBuilder(banUser, modUser, caseID, foundCase.Reason, foundCase.DateTime);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

            await context.Channel.SendMessageAsync("", embed: embed.Build());
        }
    }
}
