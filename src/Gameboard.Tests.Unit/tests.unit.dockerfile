# Use SDK to build and run
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS dev

ENV ASPNETCORE_URLS=http://*:5000 \
    ASPNETCORE_ENVIRONMENT=TEST

COPY . /app

WORKDIR /app/src/Gameboard.Tests.Unit

RUN dotnet restore

CMD ["dotnet", "test"]
