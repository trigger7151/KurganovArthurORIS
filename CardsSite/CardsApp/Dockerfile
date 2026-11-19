# Сборка из папки CardsSite (родительской для CardsApp), потому что CardsApp
# ссылается на соседние проекты фреймворка, ORM и шаблонизатора.
# Обычно запускается через docker compose (см. docker-compose.yml и README.md).

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY MiniTemplateEngine/ MiniTemplateEngine/
COPY MiniHttpServer.Framework/ MiniHttpServer.Framework/
COPY MiniHttpServer.ORM/ MiniHttpServer.ORM/
COPY CardsApp/ CardsApp/
# bin/obj с машины разработчика содержат пути Windows — собираем начисто.
RUN find . -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} + \
    && dotnet publish CardsApp/CardsApp.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
# В контейнере сервер слушает все интерфейсы, а не только localhost.
COPY --from=build /src/CardsApp/Settings/settings.docker.json ./Settings/settings.json
RUN mkdir -p /app/static/uploads
EXPOSE 8080
ENTRYPOINT ["dotnet", "CardsApp.dll"]
