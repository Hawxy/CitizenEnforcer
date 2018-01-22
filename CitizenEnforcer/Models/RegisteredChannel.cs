using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CitizenEnforcer.Models
{
    public class RegisteredChannel
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Key]
        public ulong ChannelId { get; set; }
        [Required]
        public ulong GuildId { get; set; }
        public Guild Guild { get; set; }

    }
}
