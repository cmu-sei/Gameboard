# Gameboard API

Developed by Carnegie Mellon University's Software Engineering Institute (SEI), **Gameboard** is a flexible web platform that provides game design capabilities and a competition-ready user interface. The Gameboard API works in conjunction with the [Gameboard UI](https://github.com/cmu-sei/gameboard-ui) web client to deliver a full competition environment.

## Dependencies

The Gameboard API requires the .NET Core 6.0 framework.

## Getting Started

1. Install .Net Core SDK 6.0
2. Start the application using the following command: `dotnet run`
3. Browse to `http://localhost:5000/api`

## Development `appsettings`

Review the `appsettings.conf` file.  Put alterations in `appsettings.Development.conf`, which cascades on top of `appsettings.conf`.  Recommended dev settings can be found at the bottom of the `appsettings.conf` file.

## Authentication and Authorization
The gameboard requires an OIDC Identity server. Configuring Identity is outside the scope of this Readme. However, if using our [IdentityServer](https://github.com/cmu-sei/Identity), the `test/oidc-dev.http` provides a quick configuration (using vs-code and it's Rest Client extension).

## Game Engine Dependency
The API leverages a "GameEngine" service to orchestrate any virtualized challenge resources.  This is currently implemented by the [TopoMojo Api](https://github.com/cmu-sei/TopoMojo).  You will need to an api-key from TopoMojo.
