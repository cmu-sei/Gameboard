#
# multi-stage target: dev
#
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dev

ENV ASPNETCORE_URLS=http://*:5000 \
    ASPNETCORE_ENVIRONMENT=DEVELOPMENT

WORKDIR /app
COPY . /app

WORKDIR /app/src/Gameboard.Api
RUN dotnet publish -c Release -o /app/dist
CMD ["dotnet", "run"]

#
# multi-stage target: prod
#
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS prod
ARG commit
ENV COMMIT=$commit

# install tools for PNG generation on the server
RUN apt-get update \
    && apt-get install -y --no-install-recommends wkhtmltopdf \
    && rm -rf /var/lib/apt/lists/*

# sanity check so CI fails early if package layout changes
RUN which wkhtmltoimage && wkhtmltoimage --version

COPY --from=dev /app/dist /app
COPY --from=dev /app/LICENSE.md /app/LICENSE.md

WORKDIR /app
EXPOSE 80
ENV ASPNETCORE_URLS=http://*:80
CMD [ "dotnet", "Gameboard.Api.dll" ]
