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

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace CitizenEnforcer.Models
{
    public class Guild
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public ulong GuildId { get; set; }
        public bool IsEditLoggingEnabled { get; set; }
        public bool IsModerationEnabled { get; set; }
        public bool IsPublicAnnounceEnabled { get; set; }
        public ulong LoggingChannel { get; set; }
        public ulong ModerationChannel { get; set; }
        public ulong PublicAnnouceChannel { get; set; }
        public List<RegisteredChannel> RegisteredChannels { get; set; }
        public List<ModLog> ModLogs { get; set; }

    }
}
