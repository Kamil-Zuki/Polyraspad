ARG DOTNET_ASPNET_IMAGE=mcr.microsoft.com/dotnet/aspnet:10.0
ARG DOTNET_SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:10.0

FROM ${DOTNET_ASPNET_IMAGE} AS base
USER $APP_UID
WORKDIR /app
EXPOSE 5131

FROM ${DOTNET_SDK_IMAGE} AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["AgentService.csproj", "."]
RUN dotnet restore "./AgentService.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./AgentService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./AgentService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AgentService.dll"]
