using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace CitizenEnforcer.Preconditions
{
    public class ReasonLimitAttribute : ParameterPreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, ParameterInfo parameter, object value, IServiceProvider services)
        {
            if (value is string reason && !string.IsNullOrEmpty(reason))
            {
               if(reason.Length > 1800)
                   return Task.FromResult(PreconditionResult.FromError($"Reason cannot exceed 1800 characters! Current length: {reason.Length}"));

               return Task.FromResult(PreconditionResult.FromSuccess());
            }

            return Task.FromResult(PreconditionResult.FromSuccess());
        }
    }
}
