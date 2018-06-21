# CitizenEnforcer
A Discord.Net moderation & edit/delete log bot running on .NET Core 2.1

Features: 

- Warn/Kick/TempBan/Ban/Unban commands
- Logging via detailed embeds (moderator, caseID, userID etc)
- Automatic logging of Bans & Unbans if done via Discord instead of a bot command
- Lookup of previous moderation actions against a specified user
- Automatic logging of deleted and modified messages in requested channels

Libraries used:

- [Discord.Net](https://github.com/RogueException/Discord.Net)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [SeriLog](https://serilog.net/)
- [Discord.Addons.Interactive](https://github.com/foxbot/Discord.Addons.Interactive)

If you want to use this bot yourself, you'll need to download the source and run a EF migration to generate the database, and then encrypt the database using DB Browser for SQLite.

An unencrypted database can be used for testing purposes (just press enter on startup), but any live data should be kept encrypted (per the Discord Bot ToS).

The config file sits in config/configuration.json and includes a list of owner IDs (currently unused), and bot prefix/token.
