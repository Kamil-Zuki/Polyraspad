ARG DOTNET_ASPNET_IMAGE=mcr.microsoft.com/dotnet/aspnet:8.0
ARG DOTNET_SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:8.0

FROM ${DOTNET_ASPNET_IMAGE} AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM ${DOTNET_SDK_IMAGE} AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MediaService.csproj", "."]
RUN dotnet restore "./MediaService.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./MediaService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MediaService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MediaService.dll"]
