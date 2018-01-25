using System.Threading.Tasks;
using CitizenEnforcer.Context;
using CitizenEnforcer.Preconditions;
using CitizenEnforcer.Services;
using Discord;
using Discord.Commands;

namespace CitizenEnforcer.Modules
{
    [Group("lookup")]
    [Alias("l")]
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(GuildPermission.BanMembers)]
    [RequireInitializedAccessible(InitializedType.All)]
    public class LookupModule : ModuleBase<SocketCommandContext>
    {
        public BotContext _botContext { get; set; }
        public LookupService _lookupService { get; set; }

        [Command("user")]
        [Alias("u")]
        [Priority(0)]
        [Summary("Finds previous cases of a given user")]
        public async Task LookupUser(IGuildUser user) => await _lookupService.LookupUser(Context, user);

        [Command("user")]
        [Alias("u")]
        [Priority(1)]
        [Summary("Find previous cases of a banned user")]
        public async Task LookupUser(IBan bannedUser) => await _lookupService.LookupUser(Context, bannedUser.User);

        [Command("case")]
        [Alias("c")]
        [Summary("Displays a stored case")]
        public async Task LookupCase(ulong caseID) => await _lookupService.LookupCase(Context, caseID);

    }
}
