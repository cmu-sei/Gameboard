#
#multi-stage target: dev
#
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS dev

ENV ASPNETCORE_URLS=http://*:5000 \
    ASPNETCORE_ENVIRONMENT=DEVELOPMENT

COPY . /app

WORKDIR /app/src/Gameboard.Api
RUN dotnet publish -c Release -o /app/dist
CMD ["dotnet", "run"]

#
#multi-stage target: prod
#
FROM mcr.microsoft.com/dotnet/runtime:7.0.1 AS prod
ARG commit
ENV COMMIT=$commit
COPY --from=dev /app/dist /app
COPY --from=dev /app/LICENSE.md /app/LICENSE.md
WORKDIR /app
EXPOSE 80
ENV ASPNETCORE_URLS=http://*:80
CMD [ "dotnet", "Gameboard.Api.dll" ]
