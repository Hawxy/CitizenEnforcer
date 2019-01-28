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
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace CitizenEnforcer.Preconditions
{
    public class ErrorPreventAttribute : ParameterPreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, ParameterInfo parameter, object value, IServiceProvider services)
        {
            if (value is IUser user && user.Id != context.User.Id)
            {
                var bot = await context.Guild.GetCurrentUserAsync();

                if (bot is SocketGuildUser socketBot && user is SocketGuildUser socketUser)
                    if (socketUser.Hierarchy > socketBot.Hierarchy)
                        return PreconditionResult.FromError("Nah.");
                
                return PreconditionResult.FromSuccess();
            }
               
            return PreconditionResult.FromError("You can't use this command on yourself!");

        }
    }
}