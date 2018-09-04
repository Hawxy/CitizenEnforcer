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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CitizenEnforcer.Common;
using CitizenEnforcer.Context;
using CitizenEnforcer.Models;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CitizenEnforcer.Services
{
    public class TempBanTimer
    {
        private readonly Timer _timer;
        private readonly BotContext _botContext;
        private readonly DiscordSocketClient _client;
        private readonly ModerationService _moderationService;
        private readonly IMemoryCache _banCache;
        public TempBanTimer(BotContext botContext, DiscordSocketClient client, ModerationService moderationService, IMemoryCache banCache)
        {
            _botContext = botContext;
            _client = client;
            _moderationService = moderationService;
            _banCache = banCache;
            _timer = new Timer(_ => CheckBan(), null, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(20)); 
        }

        private async void CheckBan()
        {
            List<TempBan> Unbans = await _botContext.TempBans.Include(y=> y.ModLog).Where(x => x.TempBanActive && DateTimeOffset.UtcNow > x.ExpireDate).ToListAsync();
            foreach (TempBan ban in Unbans)
            {
                //find and unban
                SocketGuild guild = _client.GetGuild(ban.ModLog.GuildId);
                var guildban = await guild.GetBanSafelyAsync(ban.ModLog.UserId);
                //the user was already unbanned so don't bother continuing
                if (guildban == null)
                {
                    ban.TempBanActive = false;
                    await _botContext.SaveChangesAsync();
                    continue;
                }
                _banCache.Set(ban.ModLog.UserId, new ModerationService.CacheModel(guild.Id), TimeSpan.FromSeconds(5));
                await guild.RemoveBanAsync(ban.ModLog.UserId);

                ban.TempBanActive = false;
                
                //log the unban
                var builder = FormatUtilities.GetUnbanBuilder(guildban.User, "TempBan Expired");

                await _moderationService.SendMessageToModLog(guild, builder);
                await _moderationService.SendMessageToAnnounce(guild, $"***{guildban.User}'s temporary ban has expired***");

            }
            await _botContext.SaveChangesAsync();
        }
    }
}
