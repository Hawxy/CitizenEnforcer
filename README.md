# CitizenEnforcer
A Discord.Net moderation & edit/delete log bot running on .NET 5.0. Created for a partnered Discord server (42,000+ users). 

[![Build Status](https://dev.azure.com/GithubHawxy/CitizenEnforcer/_apis/build/status/Hawxy.CitizenEnforcer)](https://dev.azure.com/GithubHawxy/CitizenEnforcer/_build/latest?definitionId=1)

Features: 

- Warn/Kick/TempBan/Ban/Unban commands
- Logging via detailed embeds (moderator, caseID, userID etc)
- Lookup of previous moderation actions against a specified user
- Automatic handling of temporary bans
- Automatic logging of deleted and modified messages in requested channels
- Automatic logging of user profile picture changes
- Fallback logging of Bans & Unbans if done via Discord instead of a bot command
- Lockdown and channel freeze functionality (experimental)

Libraries used:

- [Discord.Net](https://github.com/discord-net/Discord.Net)
- [Discord.Addons.Hosting](https://github.com/Hawxy/Discord.Addons.Hosting)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [Serilog](https://serilog.net/)
- [Discord.InteractivityAddon](https://github.com/Playwo/Discord.InteractivityAddon)

For support enquires please join the [ZephyrDev Discord](https://discord.gg/evXfQ9v)

**How to Use:**
The official version of this bot is only available to a handful of approved guilds, as I'm not interested in supporting a large moderation bot.

If you want to host this bot yourself, you should have an intermediate understanding of C# and EF Core. Unfortunately I don't have the time to offer newbie support in this space.

You'll need to fork + download the source and run a EF migration to generate the database, and then encrypt the database using DB Browser for SQLite.

An unencrypted database can be used for testing purposes (just press enter on startup), but any live data should be kept encrypted (per the Discord Bot ToS).

The config file sits in config/configuration.json and includes the bot's prefix and token.

**Reminder:**

This source is provided under the AGPL license, please respect the conditions and push any code changes to a fork of the repo.