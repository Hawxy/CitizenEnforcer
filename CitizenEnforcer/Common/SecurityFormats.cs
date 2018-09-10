using System;
using Discord;

namespace CitizenEnforcer.Common
{
    public static class SecurityFormats
    {
        public static EmbedBuilder GetPublicLockdownBuilder(IUser mod)
            => GetLockdownBuilder(mod).WithThumbnailUrl("https://media.giphy.com/media/l0Iy4cguQsSZzhSj6/giphy.gif")
                .WithDescription("All non-role users are restricted from speaking until lifted");

        public static EmbedBuilder GetLockdownBuilder(IUser mod)
        {
            var embedBuilder =
                new EmbedBuilder()
                    .WithColor(new Color(229, 57, 53))
                    .WithCurrentTimestamp()
                    .WithTitle($"**Emergency Lockdown Enabled**")
                    .WithAuthor(mod);
           
            return embedBuilder;
        }

        public static EmbedBuilder GetPublicLiftedBuilder(IUser mod)
            => GetLiftedBuilder(mod).WithDescription("You may now speak again");

        public static EmbedBuilder GetLiftedBuilder(IUser mod)
        {
            var embedBuilder =
                new EmbedBuilder()
                    .WithColor(new Color(56, 142, 60))
                    .WithCurrentTimestamp()
                    .WithTitle($"Emergency Lockdown Lifted")
                    .WithAuthor(mod);

            return embedBuilder;
        }

        public static EmbedBuilder GetPublicFreezeBuilder(IUser mod) 
            => GetFreezeBuilder(mod).WithDescription("This channel has been locked to non-role users");

        public static EmbedBuilder GetFreezeBuilder(IUser mod)
        {
            var embedBuilder =
                new EmbedBuilder()
                    .WithColor(new Color(253, 216, 53))
                    .WithCurrentTimestamp()
                    .WithTitle($"**Channel Freeze Enabled**")
                    .WithAuthor(mod);

            return embedBuilder;
        }

        public static EmbedBuilder GetUnfrozenBuilder(IUser mod)
        {
            var embedBuilder =
                new EmbedBuilder()
                    .WithColor(new Color(56, 142, 60))
                    .WithCurrentTimestamp()
                    .WithTitle($"Channel Freeze Disabled")
                    .WithAuthor(mod);
            return embedBuilder;
        }


    }
}
