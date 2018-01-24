using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace CitizenEnforcer.Preconditions
{
    public class NotSelfAttribute : ParameterPreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, ParameterInfo parameter, object value, IServiceProvider services)
        {
            IUser user = null;
            if (parameter.Type == typeof(IGuildUser))
                user = value as IGuildUser;
            
            if (user != null && user.Id != context.User.Id)
                return Task.FromResult(PreconditionResult.FromSuccess());
            return Task.FromResult(PreconditionResult.FromError("You can't use this command on yourself!"));

        }
    }
}
