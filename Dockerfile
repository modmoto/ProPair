FROM mcr.microsoft.com/dotnet/aspnet:11.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:11.0 AS build
WORKDIR /src
COPY ["src/ProPair.Adapters.Mongo/ProPair.Adapters.Mongo.csproj", "src/ProPair.Adapters.Mongo/"]
COPY ["src/ProPair.Adapters.NewRecruit/ProPair.Adapters.NewRecruit.csproj", "src/ProPair.Adapters.NewRecruit/"]
COPY ["src/ProPair.Adapters.Rest/ProPair.Adapters.Rest.csproj", "src/ProPair.Adapters.Rest/"]
COPY ["src/ProPair.Adapters.Sync/ProPair.Adapters.Sync.csproj", "src/ProPair.Adapters.Sync/"]
COPY ["src/ProPair.Application/ProPair.Application.csproj", "src/ProPair.Application/"]
COPY ["src/ProPair.Contracts/ProPair.Contracts.csproj", "src/ProPair.Contracts/"]
COPY ["src/ProPair.Domain/ProPair.Domain.csproj", "src/ProPair.Domain/"]
COPY ["src/ProPair.Web/ProPair.Web.csproj", "src/ProPair.Web/"]
RUN dotnet restore "src/ProPair.Adapters.Rest/ProPair.Adapters.Rest.csproj"
RUN dotnet restore "src/ProPair.Adapters.Sync/ProPair.Adapters.Sync.csproj"
COPY . .

FROM build AS publish
RUN dotnet publish "src/ProPair.Adapters.Rest/ProPair.Adapters.Rest.csproj" -c Release -o /app/publish/rest
RUN dotnet publish "src/ProPair.Adapters.Sync/ProPair.Adapters.Sync.csproj" -c Release -o /app/publish/sync

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY docker-entrypoint.sh .
# app (API + Blazor WASM)
ENV ASPNETCORE_HTTP_PORTS=80
# sync process: only reachable inside the container, its /sync/* URLs have no login
ENV SYNC_URLS=http://127.0.0.1:5090
ENTRYPOINT ["bash", "docker-entrypoint.sh"]
