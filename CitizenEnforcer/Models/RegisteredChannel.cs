using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CitizenEnforcer.Models
{
   public class RegisteredChannel
    {
        public RegisteredChannel(){}

        public RegisteredChannel(ulong channelID, ulong guildID)
        {
            ChannelId = channelID;
            GuildId = guildID;
        }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Key]
        public ulong ChannelId { get; set; }
        [Required]
        public ulong GuildId { get; set; }
        public Guild Guild { get; set; }

    }
}
