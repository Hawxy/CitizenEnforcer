using System;
using System.ComponentModel.DataAnnotations;
using CitizenEnforcer.Common;
using Discord;
using Discord.Commands;

namespace CitizenEnforcer.Models
{
    public class ModLog
    {
        //this db setup isn't perfect, but it'll do for now
        public ModLog(){}
        public ModLog(SocketCommandContext context, IUser user, ulong CaseID, InfractionType type, string reason)
        {
            ModLogCaseID = CaseID;
            GuildId = context.Guild.Id;
            UserId = user.Id;
            UserName = FormatUtilities.GetFullName(user);
            ModId = context.User.Id;
            ModName = FormatUtilities.GetFullName(context.User);
            DateTime = DateTimeOffset.UtcNow;
            InfractionType = type;
            Reason = reason;
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
