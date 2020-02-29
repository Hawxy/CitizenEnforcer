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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CitizenEnforcer.Common;
using CitizenEnforcer.Context;
using CitizenEnforcer.Models;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CitizenEnforcer.Services
{
    public class TempBanTimer : IHostedService, IAsyncDisposable
    {
        private Timer _timer;
        private readonly BotContext _botContext;
        private readonly DiscordSocketClient _client;
        private readonly ModerationService _moderationService;
        private readonly IMemoryCache _banCache;
        private readonly ILogger<TempBanTimer> _logger;

        public TempBanTimer(BotContext botContext, DiscordSocketClient client, ModerationService moderationService, IMemoryCache banCache, ILogger<TempBanTimer> logger)
        {
            _botContext = botContext;
            _client = client;
            _moderationService = moderationService;
            _banCache = banCache;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(_ => CheckBan(), null, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(20));
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private async void CheckBan()
        {
            _logger.LogDebug("Checking for expired tempbans....");
            List<TempBan> Unbans = await _botContext.TempBans.Include(y=> y.ModLog).AsAsyncEnumerable().Where(x => x.TempBanActive && DateTimeOffset.UtcNow > x.ExpireDate).ToListAsync();
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
                _banCache.Set(ban.ModLog.UserId, new ModerationService.CacheModel(guild.Id, ModerationService.CacheType.UnbanReject), TimeSpan.FromSeconds(5));
                await guild.RemoveBanAsync(ban.ModLog.UserId);

                ban.TempBanActive = false;
                
                //log the unban
                var builder = ModeratorFormats.GetUnbanBuilder(guildban.User, "TempBan Expired");

                await _moderationService.SendEmbedToModLog(guild, builder);
                await _moderationService.SendMessageToAnnounce(guild, $"***{guildban.User}'s temporary ban has expired***");

            }
            await _botContext.SaveChangesAsync();
        }

        public ValueTask DisposeAsync()
        {
            return _timer.DisposeAsync();
        }
    }
}
