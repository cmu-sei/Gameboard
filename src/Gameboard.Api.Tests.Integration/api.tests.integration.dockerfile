FROM mcr.microsoft.com/dotnet/sdk:7.0 AS dev

ENV ASPNETCORE_URLS=http://*:5000 \
    ASPNETCORE_ENVIRONMENT=Test

COPY . /app

WORKDIR /app/src/Gameboard.Api.Tests.Integration

RUN dotnet restore

CMD ["dotnet", "test"]
