﻿using System;
using System.Collections.Generic;
using System.Text;
using CitizenEnforcer.Models;
using Discord;

namespace CitizenEnforcer.Common
{
    public static class FormatUtilities
    {
        public static string Prefix;
        public static string GetFullName(IUser user) => $"{user.Username}#{user.Discriminator}";

        public static EmbedBuilder GetWarnBuilder(IUser user, IUser mod, ulong caseID, string reason, DateTimeOffset postedDate)
        {
            var manbuilder =
                new EmbedBuilder().WithColor(new Color(2, 136, 209))
                    .WithAuthor(mod).WithTimestamp(postedDate).WithTitle($"Warned User - Entry ID: {caseID}").WithDescription("---------------------");

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
                z.Value = reason ?? $"Responsible Moderator, please type ``{Prefix}r {caseID}`` to add a reason";
            });

            return manbuilder;
        }

        public static EmbedBuilder GetKickBuilder(IUser user, IUser mod, ulong caseID, string reason, DateTimeOffset postedDate)
        {
            var manbuilder =
                new EmbedBuilder().WithColor(new Color(253, 216, 53))
                    .WithAuthor(mod).WithTimestamp(postedDate).WithTitle($"Kicked User - Entry ID: {caseID}").WithDescription("---------------------");

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
                z.Value = reason ?? $"Responsible Moderator, please type ``{Prefix}r {caseID}`` to add a reason";
            });
            return manbuilder;
        }

        public static EmbedBuilder GetTempBanBuilder(IUser user, IUser mod, ulong caseID, string reason, DateTimeOffset postedDate, DateTimeOffset endTime)
        {
            var manbuilder =
                new EmbedBuilder().WithColor(new Color(251, 140, 0))
                    .WithAuthor(mod).WithTimestamp(postedDate).WithTitle($"Temp-Banned User - Entry ID: {caseID}").WithDescription("---------------------");

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
                z.Value = $"{endTime.DateTime} UTC";
            });
            manbuilder.AddField(z =>
            {
                z.Name = "Reason";
                z.Value = reason ?? $"Responsible Moderator, please type ``{Prefix}r {caseID}`` to add a reason";
            });

            return manbuilder;
        }

        public static EmbedBuilder GetBanBuilder(IUser user, IUser mod, ulong caseID, string reason, DateTimeOffset postedDate)
        {
            var manbuilder =
                new EmbedBuilder().WithColor(new Color(229, 57, 53))
                    .WithTimestamp(postedDate).WithTitle($"Banned User - Entry ID: {caseID}").WithDescription("---------------------");
            if (mod == null)
                manbuilder.WithAuthor("Unknown Moderator");
            else
                manbuilder.WithAuthor(mod);
            

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
                z.Value = reason ?? $"Responsible Moderator, please type ``{Prefix}r {caseID}`` to add a reason";
            });

            return manbuilder;
        }

        public static EmbedBuilder GetUnbanBuilder(IUser user, string titleReason, IUser resMod = null)
        {
            var manbuilder =
                new EmbedBuilder().WithColor(new Color(56, 142, 60))
                    .WithCurrentTimestamp().WithTitle($"{titleReason} - Unbanned User").WithDescription("---------------------");
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

        public static EmbedBuilder GetUserLookupBuilder(IUser user, List<ModLog> cases, InfractionType infraction, bool isBanned)
        {
            var manbuilder =
                new EmbedBuilder().WithColor(new Color(2, 136, 209))
                    .WithCurrentTimestamp().WithTitle($"User Lookup - {GetFullName(user)} - {user.Id}").WithDescription("---------------------");
           
            manbuilder.AddField(z =>
            {
                z.Name = "Currently Banned";
                z.Value = isBanned;
                z.IsInline = true;
            });
            manbuilder.AddField(z =>
            {
                z.Name = "Highest Infraction";
                z.Value = infraction.ToString();
                z.IsInline = true;
            });

            StringBuilder builder = new StringBuilder();
            cases.ForEach(x=> builder.Append($"\n{x.ModLogCaseID} - {x.InfractionType}"));  

            manbuilder.AddField(z =>
            {
                z.Name = "Related Case IDs:";
                z.Value = $"```{builder.ToString()}```";
                z.IsInline = false;
            });

            return manbuilder;
        }
    }
}
