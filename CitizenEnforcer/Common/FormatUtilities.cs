﻿using System;
using Discord;

namespace CitizenEnforcer.Common
{
    public static class FormatUtilities
    {
        public static string GetFullName(IUser user) => $"{user.Username}#{user.Discriminator}";

        public static EmbedBuilder GetWarnBuilder(IUser user, IUser mod, ulong caseID, string reason)
        {
            var manbuilder =
                new EmbedBuilder().WithColor(new Color(2, 136, 209))
                    .WithAuthor(mod).WithCurrentTimestamp().WithTitle($"Warned User - Entry ID: {caseID}").WithDescription("---------------------");

            manbuilder.AddField(z =>
            {
                z.Name = "User Name";
                z.Value = GetFullName(user);
                z.IsInline = true;
            });
            manbuilder.AddField(z =>
            {
                z.Name = "UserID";
                z.Value = user.Id;
                z.IsInline = true;
            });
            manbuilder.AddField(z =>
            {
                z.Name = "Reason";
                z.Value = reason ?? $"Responsible Moderator, please type ``^r {caseID}`` to add a reason";
            });

            return manbuilder;
        }

        public static EmbedBuilder GetKickBuilder(IUser user, IUser mod, ulong caseID, string reason)
        {
            var manbuilder =
                new EmbedBuilder().WithColor(new Color(253, 216, 53))
                    .WithAuthor(mod).WithCurrentTimestamp().WithTitle($"Kicked User - Entry ID: {caseID}").WithDescription("---------------------");

            manbuilder.AddField(z =>
            {
                z.Name = "User Name";
                z.Value = GetFullName(user);
                z.IsInline = true;
            });
            manbuilder.AddField(z =>
            {
                z.Name = "UserID";
                z.Value = user.Id;
                z.IsInline = true;
            });
            manbuilder.AddField(z =>
            {
                z.Name = "Reason";
                z.Value = reason ?? $"Responsible Moderator, please type ``^r {caseID}`` to add a reason";
            });
            return manbuilder;
        }

        public static EmbedBuilder GetTempBanBuilder(IUser user, IUser mod, ulong caseID, string reason, DateTime endTime)
        {
            var manbuilder =
                new EmbedBuilder().WithColor(new Color(251, 140, 0))
                    .WithAuthor(mod).WithCurrentTimestamp().WithTitle($"User Temp-Banned - Entry ID: {caseID}").WithDescription("---------------------");

            manbuilder.AddField(z =>
            {
                z.Name = "User Name";
                z.Value = GetFullName(user);
                z.IsInline = true;
            });
            manbuilder.AddField(z =>
            {
                z.Name = "UserID";
                z.Value = user.Id;
                z.IsInline = true;
            });
            manbuilder.AddField(z =>
            {
                z.Name = "Expiry Date & Time";
                z.Value = $"{endTime} UTC";
            });
            manbuilder.AddField(z =>
            {
                z.Name = "Reason";
                z.Value = reason ?? $"Responsible Moderator, please type ``^r {caseID}`` to add a reason";
            });

            return manbuilder;
        }

        public static EmbedBuilder GetBanBuilder(IUser user, IUser mod, ulong caseID, string reason)
        {
            var manbuilder =
                new EmbedBuilder().WithColor(new Color(229, 57, 53))
                    .WithAuthor(mod).WithCurrentTimestamp().WithTitle($"User Banned - Entry ID: {caseID}").WithDescription("---------------------");

            manbuilder.AddField(z =>
            {
                z.Name = "User Name";
                z.Value = GetFullName(user);
                z.IsInline = true;
            });
            manbuilder.AddField(z =>
            {
                z.Name = "UserID";
                z.Value = user.Id;
                z.IsInline = true;
            });
            manbuilder.AddField(z =>
            {
                z.Name = "Reason";
                z.Value = reason ?? $"Responsible Moderator, please type ``^r {caseID}`` to add a reason";
            });

            return manbuilder;
        }

        public static EmbedBuilder GetUnbanBuilder(IUser user, string titleReason, IUser resMod = null)
        {
            var manbuilder =
                new EmbedBuilder().WithColor(new Color(56, 142, 60))
                    .WithCurrentTimestamp().WithTitle($"{titleReason} - User Unbanned").WithDescription("---------------------");
            if (resMod != null)
                manbuilder.WithAuthor(resMod);

            manbuilder.AddField(z =>
            {
                z.Name = "User Name";
                z.Value = GetFullName(user);
                z.IsInline = true;
            });
            manbuilder.AddField(z =>
            {
                z.Name = "UserID";
                z.Value = user.Id;
                z.IsInline = true;
            });

            return manbuilder;
        }
    }
}