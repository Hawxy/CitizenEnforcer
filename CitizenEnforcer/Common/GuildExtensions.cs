using System.Threading.Tasks;
using Discord;
using Discord.Net;

namespace CitizenEnforcer.Common
{
    public static class GuildExtensions
    {
        //prevents needless try/catches over the codebase
        public static async Task<IBan> GetBanSafelyAsync(this IGuild guild, ulong userID)
        {
            try
            {
                var ban = await guild.GetBanAsync(userID);
                return ban;
            }
            catch (HttpException)
            {
                return null;
            }
        }
    }
}
