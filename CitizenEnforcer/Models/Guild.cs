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
