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

namespace CitizenEnforcer.Models
{
    public class TempBan
    {
        public ulong TempBanId { get; set; }
        public ulong ModLogDBId { get; set; }
        public ModLog ModLog { get; set; }
        public bool TempBanActive { get; set; }
        public DateTimeOffset ExpireDate { get; set; }
    }
}
