# CitizenEnforcer
A basic Discord .NET moderation & edit/delete log bot. Logs moderation actions to a SQLite database.

If you want to use this bot yourself, you'll need to download the source and run a EF migration to generate the database, and then encrypt the database using DB Browser.

An unencrypted database can be used for testing purposes (just press enter on startup), but any live data should be kept encrypted (read the Discord Bot ToS!)

The config file sits in config/configuration.json and includes a list of owner IDs, and bot prefix/token.
