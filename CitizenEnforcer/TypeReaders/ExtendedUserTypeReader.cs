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
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace CitizenEnforcer.TypeReaders
{
    //Handles users that have left the guild
    public class ExtendedUserTypeReader<T> : UserTypeReader<T> where T : class, IUser
    {
        public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            var res = await base.ReadAsync(context, input, services);
            if (res.IsSuccess) return res;

            if (ulong.TryParse(input, out ulong result) && context is SocketCommandContext ctx)
            {
                var user = await ctx.Client.Rest.GetUserAsync(result);

                if (user != null)
                    return TypeReaderResult.FromSuccess(user);
            }

            return TypeReaderResult.FromError(CommandError.ObjectNotFound,  "Unable to find user");
        }
    }
}
