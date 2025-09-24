# Gameboard

Developed by Carnegie Mellon University's Software Engineering Institute (SEI), **Gameboard** is a flexible web platform that provides game design capabilities and a competition-ready user interface. The Gameboard API works in conjunction with the [Gameboard web client](https://github.com/cmu-sei/gameboard-ui) to deliver a full competition environment.

## Dependencies

The Gameboard API requires the .NET Core 8.0 framework, an identity provider, a PostgreSQL database, and a "game engine" (described below).

### .NET Core

You can download [.NET Core](https://dotnet.microsoft.com/en-us/download) from its official website or using an OS package manager. The current version of Gameboard requires .NET Core 8.0.

### Identity Provider

Gameboard is compatible with any OIDC-compliant identity provider, including KeyCloak, Okta, Auth0, or any platform-based third-party provider (e.g. Google, Meta, etc.). Configure Gameboard's IDP using the `appsettings.conf` file.

### Postgres

The API relies on a PostgreSQL database to persist its data. Supply connection information to Gameboard via the `Database__` settings in `appsettings.conf`.

### Game Engine

The API leverages a "game engine" service to orchestrate virtualized challenge resources. Currently, the only officially-supported engine is [TopoMojo](https://github.com/cmu-sei/TopoMojo). You'll need to an API key from an active TopoMojo app to connect it with Gameboard.

## Development `appsettings`

Review the `appsettings.conf` file. Put alterations in `appsettings.Development.conf`, which cascades on top of `appsettings.conf`. Recommended dev settings can be found at the bottom of the `appsettings.conf` file.

## Getting Started

1. Install and configure dependencies
2. Create `appsettings.Development.conf` and set appropriate values for your IDP, database, and game engine
3. Start the application using the following command: `dotnet run` (or use an IDE's debug tools)
4. Browse to `http://localhost:5002/api`.

## User documentation

Gameboard is part of the [Crucible](https://cmu-sei.github.io/crucible/) framework. We maintain comprehensive documentation for all Crucible apps, including Gameboard, there. [Check it out!](https://cmu-sei.github.io/crucible/gameboard/)

## Reporting bugs and requesting features

Think you found a bug? Please report all Crucible bugs - including bugs for the individual Crucible apps - in the [cmu-sei/crucible issue tracker](https://github.com/cmu-sei/crucible/issues).

Include as much detail as possible including steps to reproduce, specific app involved, and any error messages you may have received.

Have a good idea for a new feature? Submit all new feature requests through the [cmu-sei/crucible issue tracker](https://github.com/cmu-sei/crucible/issues).

Include the reasons why you're requesting the new feature and how it might benefit other Crucible users.
