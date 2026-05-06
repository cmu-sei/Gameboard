#
# multi-stage target: dev
#
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dev
ARG VERSION


ENV ASPNETCORE_URLS=http://*:5000 \
    ASPNETCORE_ENVIRONMENT=DEVELOPMENT

WORKDIR /app
COPY . /app

WORKDIR /app/src/Gameboard.Api
RUN dotnet publish -c Release -o /app/dist /p:Version=${VERSION:-1.0.0} /p:AssemblyVersion=${VERSION:-1.0.0}
CMD ["dotnet", "run"]

#
# multi-stage target: prod
#
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS prod
ARG commit
ENV COMMIT=$commit

# install tools for PNG generation on the server
ARG TARGETARCH
RUN apt-get update && apt-get install -y wget && apt-get clean \
    && ARCH="${TARGETARCH:-amd64}" \
    && [ "$ARCH" = "arm64" ] || ARCH="amd64" \
    && wget -O ~/wkhtmltopdf.deb "https://github.com/wkhtmltopdf/packaging/releases/download/0.12.6.1-3/wkhtmltox_0.12.6.1-3.bookworm_${ARCH}.deb" \
    && apt-get install -y ~/wkhtmltopdf.deb \
    && rm ~/wkhtmltopdf.deb

# sanity check so CI fails early if package layout changes
RUN which wkhtmltoimage && wkhtmltoimage --version

COPY --from=dev /app/dist /app
COPY --from=dev /app/LICENSE.md /app/LICENSE.md

WORKDIR /app
EXPOSE 80
ENV ASPNETCORE_URLS=http://*:80
CMD [ "dotnet", "Gameboard.Api.dll" ]
