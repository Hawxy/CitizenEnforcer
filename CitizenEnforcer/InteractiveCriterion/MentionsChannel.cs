using System.Threading.Tasks;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;

namespace CitizenEnforcer.InteractiveCriterion
{
    internal class MentionsChannel : ICriterion<SocketMessage>
    {
        public Task<bool> JudgeAsync(SocketCommandContext sourceContext, SocketMessage parameter)
        {
            bool ok = parameter.MentionedChannels.Count != 0 &&
                      sourceContext.User.Id == parameter.Author.Id &&
                      sourceContext.Channel.Id == parameter.Channel.Id;
            return Task.FromResult(ok);
        }
    }
}
