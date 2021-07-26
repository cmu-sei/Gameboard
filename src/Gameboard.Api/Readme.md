# Gameboard

A web-api to serve games and challenges

# Get Started

Run the app and browse to `http://localhost:5000/api`.  Or see `Controllers/Tests` for REST Client tests.

# Configuration

See the `appsettings.conf` for info on settings.

By default, the app uses an in-memory database, so no persistence. Set appsettings for  `Database:Provider` and `Database:ConnectionString`.

# Data Migrations
Install tooling: `dotnet tool install dotnet-ef --global`

From the project root, run add/undo script (.sh or .ps1) in Data/Scripts:
```bash
% bash Data/Scripts/migrations-add.sh name
```
