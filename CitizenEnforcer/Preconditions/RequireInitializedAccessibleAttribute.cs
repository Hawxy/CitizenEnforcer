using System;
using System.Threading.Tasks;
using CitizenEnforcer.Context;
using CitizenEnforcer.Models;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CitizenEnforcer.Preconditions
{
    public class RequireInitializedAccessibleAttribute : PreconditionAttribute
    {
        private readonly InitializedType _type;
        public RequireInitializedAccessibleAttribute(InitializedType type)
        {
            _type = type;
        }
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            Guild guild = null;
            if (_type == InitializedType.Basic || _type == InitializedType.All)
            {
                var botContext = services.GetService<BotContext>();
                guild = await botContext.Guilds.FirstOrDefaultAsync(x => x.GuildId == context.Guild.Id);
                if (guild == null)
                    return PreconditionResult.FromError("Failure: This bot has not been setup on this server, use ``^setup initialize``");
            }
            if (_type == InitializedType.All)
            {
                if (!guild.IsModerationEnabled)
                    return PreconditionResult.FromError("Failure: Moderation tools are disabled. Enable them with ``^setup IsModerationEnabled true``");

                var channel = await context.Guild.GetChannelAsync(guild.ModerationChannel);
                var user = await context.Guild.GetCurrentUserAsync();
                var perms = user.GetPermissions(channel);
                if (!perms.ViewChannel || !perms.SendMessages || !perms.EmbedLinks)
                    return PreconditionResult.FromError("Failure: Unable to access or write to moderation channel. Change channel permissions or set a new one with ``^setup ModerationChannel``");
            }
            return PreconditionResult.FromSuccess();
        }
    }
}
