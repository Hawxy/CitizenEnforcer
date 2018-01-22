using System;

namespace CitizenEnforcer.Models
{
    public class TempBan
    {
        public ulong TempBanId { get; set; }
        public ulong ModLogDBId { get; set; }
        public ModLog ModLog { get; set; }
        public bool TempBanActive { get; set; }
        public DateTime ExpireDate { get; set; }

    }
}
