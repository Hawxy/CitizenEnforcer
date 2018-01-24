using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CitizenEnforcer.Common;
using CitizenEnforcer.Context;
using CitizenEnforcer.Models;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace CitizenEnforcer.Services
{
    public class TempBanTimer
    {
        private Timer _timer;
        private readonly BotContext _botContext;
        private readonly DiscordSocketClient _client;
        private readonly ModerationService _moderationService;
        public TempBanTimer(BotContext botContext, DiscordSocketClient client, ModerationService moderationService)
        {
            _botContext = botContext;
            _client = client;
            _moderationService = moderationService;
            _timer = new Timer(_ => CheckBan(), null, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(20)); 
        }

        private async void CheckBan()
        {
            List<TempBan> Unbans = await _botContext.TempBans.Include(y=> y.ModLog).Where(x => x.TempBanActive && DateTimeOffset.UtcNow > x.ExpireDate).ToListAsync();
            foreach (TempBan ban in Unbans)
            {
                //find and unban
                SocketGuild guild = _client.GetGuild(ban.ModLog.GuildId);
                var guildban = (await guild.GetBansAsync()).FirstOrDefault(x => x.User.Id == ban.ModLog.UserId);
                //the user was already unbanned so don't bother continuing
                if (guildban == null)
                {
                    ban.TempBanActive = false;
                    await _botContext.SaveChangesAsync();
                    continue;
                }
                await guild.RemoveBanAsync(ban.ModLog.UserId);

                ban.TempBanActive = false;
                
                //log the unban
                var builder = FormatUtilities.GetUnbanBuilder(guildban.User, "TempBan Expired");

                await _moderationService.SendMessageToModLog(guild, builder);

            }
            await _botContext.SaveChangesAsync();
        }
    }
}
