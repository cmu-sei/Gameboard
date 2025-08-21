#
# multi-stage target: dev
#
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS dev

ENV ASPNETCORE_URLS=http://*:5000 \
    ASPNETCORE_ENVIRONMENT=DEVELOPMENT

COPY . /app

WORKDIR /app/src/Gameboard.Api
RUN dotnet publish -c Release -o /app/dist
CMD ["dotnet", "run"]

#
# multi-stage target: prod
#
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS prod
ARG commit
ENV COMMIT=$commit

# install tools for PNG generation on the server
RUN apt-get update && apt-get install -y wkhtmltopdf && apt-get clean

COPY --from=dev /app/dist /app
COPY --from=dev /app/LICENSE.md /app/LICENSE.md
WORKDIR /app
EXPOSE 80
ENV ASPNETCORE_URLS=http://*:80
CMD [ "dotnet", "Gameboard.Api.dll" ]
