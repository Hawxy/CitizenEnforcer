using System;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;

namespace CitizenEnforcer.Common
{
    public static class Extensions
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

        public static string ResolveAtString(this SocketUserMessage message, string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            int index = message.Content.IndexOf(input, StringComparison.Ordinal);
            return message.Resolve(index);
        }
    }
}
