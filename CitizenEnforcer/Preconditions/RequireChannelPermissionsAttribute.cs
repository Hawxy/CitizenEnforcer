using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace CitizenEnforcer.Preconditions
{
    public class RequireChannelPermissionsAttribute : ParameterPreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, ParameterInfo parameter, object value, IServiceProvider services)
        {
            if (value is ITextChannel channel && context is SocketCommandContext socketContext)
            {
                var perms = socketContext.Guild.CurrentUser.GetPermissions(channel);
                if (!perms.SendMessages || !perms.EmbedLinks)
                {
                    return Task.FromResult(PreconditionResult.FromError("CE cannot send messages or embed links in the provided channel. Please fix the permissions and try again."));
                }
                return Task.FromResult(PreconditionResult.FromSuccess());
            }
            return Task.FromResult(PreconditionResult.FromError("The provided channel was not a text channel, exiting..."));
        }
    }
}
