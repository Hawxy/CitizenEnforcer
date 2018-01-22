using System;
using System.Linq;
using System.Threading.Tasks;
using CitizenEnforcer.Common;
using Discord.Commands;

namespace CitizenEnforcer.TypeReaders
{
    public class IBanTypeReader : TypeReader
    {
        public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            var bans = await context.Guild.GetBansAsync();

            if (ulong.TryParse(input, out ulong result))
            {
                var ban = bans.FirstOrDefault(x => x.User.Id == result);
                if (ban != null)
                    return TypeReaderResult.FromSuccess(ban);
            }
            else
            {
                var banuser = bans.FirstOrDefault(x => x.User.Username == input || FormatUtilities.GetFullName(x.User).Equals(input));
                if(banuser != null)
                    return TypeReaderResult.FromSuccess(banuser);
            }

            return TypeReaderResult.FromError(CommandError.ParseFailed, "Unable to find banned user");
        }
    }
}
