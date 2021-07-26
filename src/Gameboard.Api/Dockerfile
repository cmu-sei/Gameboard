#
#multi-stage target: dev
#
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS dev

ENV ASPNETCORE_URLS=http://*:5000 \
    ASPNETCORE_ENVIRONMENT=DEVELOPMENT
WORKDIR /app

COPY src/Gameboard/Gameboard.csproj /app/src/Gameboard
RUN dotnet restore

COPY . /app
RUN dotnet publish -c Release -o /app/dist

CMD ["dotnet", "run"]

#
#multi-stage target: prod
#
FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS prod
ARG commit
ENV COMMIT=$commit
COPY --from=dev /app/dist /app
COPY --from=dev /app/LICENSE.md /app/LICENSE.md
WORKDIR /app
EXPOSE 80
ENV ASPNETCORE_URLS=http://*:80
CMD [ "dotnet", "Gameboard.dll" ]
