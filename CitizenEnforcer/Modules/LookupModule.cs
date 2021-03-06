﻿#region License
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

using System.Threading.Tasks;
using CitizenEnforcer.Preconditions;
using CitizenEnforcer.Services;
using Discord;
using Discord.Commands;

namespace CitizenEnforcer.Modules
{
    [Group("lookup")]
    [Alias("l")]
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(GuildPermission.BanMembers)]
    [RequireInitialized(InitializedType.All)]
    public class LookupModule : ModuleBase<SocketCommandContext>
    {
        private readonly LookupService _lookupService;

        public LookupModule(LookupService lookupService)
        {
            _lookupService = lookupService;
        }

        [Command("user")]
        [Alias("u")]
        [Priority(1000)]
        [Summary("Finds previous cases of a given user")]
        public Task LookupUser(IUser user) => _lookupService.LookupUser(Context, user);

        [Command("user")]
        [Alias("u")]
        [Priority(500)]
        [Summary("Find previous cases of a banned user")]
        public Task LookupUser(IBan bannedUser) => _lookupService.LookupUser(Context, bannedUser.User);

        [Command("user")]
        [Alias("u")]
        [Priority(100)]
        [Summary("Find previous cases of a given user ID")]
        public Task LookupUser(ulong userId) => _lookupService.LookupUser(Context, userId);

        [Command("case")]
        [Alias("c")]
        [Summary("Displays a stored case")]
        public Task LookupCase(ulong caseID) => _lookupService.LookupCase(Context, caseID);

    }
}
