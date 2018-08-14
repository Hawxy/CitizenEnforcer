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
using System.ComponentModel.DataAnnotations;
using Discord;
using Discord.Commands;

namespace CitizenEnforcer.Models
{
    public class ModLog
    {
        //this db setup isn't perfect, but it'll do for now
        public ModLog(){}
        //For command-caused logs
        public ModLog(SocketCommandContext context, IUser user, ulong CaseID, InfractionType type, string reason) :
            this(context.User, context.Guild.Id, user, CaseID, type, reason){}

        public ModLog(IUser modUser, ulong guildID, IUser user, ulong CaseID, InfractionType type, string reason)
        {
            ModLogCaseID = CaseID;
            GuildId = guildID;
            UserId = user.Id;
            UserName = user.ToString();
            ModId = modUser.Id;
            ModName = modUser.ToString();
            DateTime = DateTimeOffset.UtcNow;
            InfractionType = type;
            Reason = reason;
        }
        //For event-caused logs that don't have a full dataset
        public ModLog(ulong CaseID, ulong GuildID, IUser user, InfractionType type)
        {
            ModLogCaseID = CaseID;
            GuildId = GuildID;
            UserId = user.Id;
            UserName = user.ToString();
            ModId = 0;
            ModName = "Unknown";
            DateTime = DateTimeOffset.UtcNow;
            InfractionType = type;
        }
        

        //DB-side primary key
        [Key]
        public ulong ModLogDBId { get; set; }
        public ulong GuildId { get; set; }
        public Guild Guild { get; set; }
        //local ID calculated on a per-server basis
        public ulong ModLogCaseID { get; set; }
        public ulong UserId { get; set; }
        [Required]
        public string UserName { get; set; }
        public ulong ModId { get; set; }
        [Required]
        public string ModName { get; set; }
        [Required]
        public DateTimeOffset DateTime { get; set; }
        [Required]
        public InfractionType InfractionType { get; set; }
        public string Reason { get; set; }
        public ulong LoggedMessageId { get; set; }
        public TempBan TempBan { get; set; }

    }

    public enum InfractionType
    {
        Warning,
        Kick,
        TempBan,
        Ban
    }
}
