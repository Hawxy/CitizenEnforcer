#region License
/*CitizenEnforcer - Moderation and logging bot
Copyright(C) 2018 Hawx
https://github.com/Hawxy/CitizenEnforcer

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published
by the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program.If not, see http://www.gnu.org/licenses/ */
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using CitizenEnforcer.Models;
using Discord;

namespace CitizenEnforcer.Common
{
    public static class ModeratorFormats
    {
        public static string Prefix;


        public static EmbedBuilder GetWarnBuilder(IUser user, IUser mod, ulong caseID, string reason, DateTimeOffset postedDate) => 
            GetWarnBuilder(user.ToString(), user.Id, mod, caseID, reason, postedDate);
        public static EmbedBuilder GetWarnBuilder(string username, ulong userID, IUser mod, ulong caseID, string reason, DateTimeOffset postedDate)
        {
            var manbuilder =
                new EmbedBuilder().WithColor(new Color(2, 136, 209))
                    .WithAuthor(mod).WithTimestamp(postedDate).WithTitle($"Warned User - Entry ID: {caseID}").WithDescription("---------------------");

            manbuilder.AddField(z =>
            {
                z.Name = "User Name";
                z.Value = username;
                z.IsInline = true;
            });
            manbuilder.AddField(z =>
            {
                z.Name = "UserID";
                z.Value = userID;
                z.IsInline = true;
            });
            manbuilder.AddField(z =>
            {
                z.Name = "Reason";
                z.Value = reason ?? $"Responsible Moderator, please type ``{Prefix}r {caseID}`` to add a reason";
            });

            return manbuilder;
        }

        public static EmbedBuilder GetKickBuilder(IUser user, IUser mod, ulong caseID, string reason, DateTimeOffset postedDate) => 
            GetKickBuilder(user.ToString(), user.Id, mod, caseID, reason, postedDate);
        public static EmbedBuilder GetKickBuilder(string username, ulong userID, IUser mod, ulong caseID, string reason, DateTimeOffset postedDate)
        {
            var manbuilder =
                new EmbedBuilder().WithColor(new Color(253, 216, 53))
                    .WithAuthor(mod).WithTimestamp(postedDate).WithTitle($"Kicked User - Entry ID: {caseID}").WithDescription("---------------------");

            manbuilder.AddField(z =>
            {
                z.Name = "User Name";
                z.Value = username;
                z.IsInline = true;
            });
            manbuilder.AddField(z =>
            {
                z.Name = "UserID";
                z.Value = userID;
                z.IsInline = true;
            });
            manbuilder.AddField(z =>
            {
                z.Name = "Reason";
                z.Value = reason ?? $"Responsible Moderator, please type ``{Prefix}r {caseID}`` to add a reason";
            });
            return manbuilder;
        }

        public static EmbedBuilder GetTempBanBuilder(IUser user, IUser mod, ulong caseID, string reason, DateTimeOffset postedDate, DateTimeOffset endTime) =>
            GetTempBanBuilder(user.ToString(), user.Id, mod, caseID, reason, postedDate, endTime);
        public static EmbedBuilder GetTempBanBuilder(string username, ulong userID, IUser mod, ulong caseID, string reason, DateTimeOffset postedDate, DateTimeOffset endTime)
        {
            var manbuilder =
                new EmbedBuilder().WithColor(new Color(251, 140, 0))
                    .WithAuthor(mod).WithTimestamp(postedDate).WithTitle($"Temp-Banned User - Entry ID: {caseID}").WithDescription("---------------------");

            manbuilder.AddField(z =>
            {
                z.Name = "User Name";
                z.Value = username;
                z.IsInline = true;
            });
            manbuilder.AddField(z =>
            {
                z.Name = "UserID";
                z.Value = userID;
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

        public static EmbedBuilder GetBanBuilder(IUser user, IUser mod, ulong caseID, string reason, DateTimeOffset postedDate) =>
            GetBanBuilder(user.ToString(), user.Id, mod, caseID, reason, postedDate);
        public static EmbedBuilder GetBanBuilder(string username, ulong userID, IUser mod, ulong caseID, string reason, DateTimeOffset postedDate)
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
                z.Value = username;
                z.IsInline = true;
            });
            manbuilder.AddField(z =>
            {
                z.Name = "UserID";
                z.Value = userID;
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
                z.Value = user.ToString();
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

        public static EmbedBuilder GetUserLookupBuilder(string username, ulong userId, List<ModLog> cases, InfractionType infraction, bool isBanned)
        {
            var manbuilder =
                new EmbedBuilder().WithColor(new Color(2, 136, 209))
                    .WithCurrentTimestamp().WithTitle($"User Lookup - {username} - {userId}").WithDescription("---------------------");

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
            cases.ForEach(x => builder.Append($"\n{x.ModLogCaseID} - {x.InfractionType}"));

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
