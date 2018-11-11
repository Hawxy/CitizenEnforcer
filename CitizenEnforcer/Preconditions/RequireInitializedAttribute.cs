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
using System.Threading.Tasks;
using CitizenEnforcer.Context;
using CitizenEnforcer.Models;
using Discord.Commands;
using EFSecondLevelCache.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CitizenEnforcer.Preconditions
{
    public class RequireInitializedAttribute : PreconditionAttribute
    {
        private readonly InitializedType _type;
        public RequireInitializedAttribute(InitializedType type)
        {
            _type = type;
        }
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            IConfiguration config = services.GetService<IConfiguration>();
            Guild guild = null;
            if (_type >= InitializedType.Basic)
            {
                var botContext = services.GetService<BotContext>();
                guild = await botContext.Guilds.AsNoTracking().Cacheable().FirstOrDefaultAsync(x => x.GuildId == context.Guild.Id);
                if (guild == null)
                    return PreconditionResult.FromError($"Failure: This bot has not been setup on this server, use ``{config["Prefix"]}setup initialize``");
            }
            if (_type == InitializedType.All)
            {
                if (!guild.IsModerationEnabled)
                    return PreconditionResult.FromError($"Failure: Moderation tools are disabled. Enable them with ``{config["Prefix"]}setup IsModerationEnabled true``");
               
            }
            return PreconditionResult.FromSuccess();
        }
    }
}
