#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG VERSION=1.0.0
ARG BUILD_NUMBER=0
ARG BUILD_DATE
ARG FULL_VERSION
WORKDIR /src
COPY ["src/Amendment.Server/Amendment.Server.csproj", "src/Amendment.Server/"]
COPY ["src/Amendment.Server.Handlers/Amendment.Server.Mediator.csproj", "src/Amendment.Server.Handlers/"]
COPY ["src/Amendment.Service/Amendment.Service.csproj", "src/Amendment.Service/"]
COPY ["src/Amendment.Repository/Amendment.Repository.csproj", "src/Amendment.Repository/"]
COPY ["src/Amendment.Model/Amendment.Model.csproj", "src/Amendment.Model/"]
COPY ["src/Amendment.Shared/Amendment.Shared.csproj", "src/Amendment.Shared/"]
COPY ["src/Amendment.Client/Amendment.Client.csproj", "src/Amendment.Client/"]
COPY ["src/Amendment.Client.Repository/Amendment.Client.Repository.csproj", "src/Amendment.Client.Repository/"]
RUN dotnet restore "./src/Amendment.Server/./Amendment.Server.csproj"
COPY . .
WORKDIR "/src/src/Amendment.Server"
RUN dotnet build "./Amendment.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build \
    -p:VERSION="$VERSION" \
    -p:BUILD_NUMBER="$BUILD_NUMBER" \
    -p:BUILD_DATE="$BUILD_DATE" \
    -p:FULL_VERSION="$FULL_VERSION"

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
ARG VERSION=1.0.0
ARG BUILD_NUMBER=0
ARG BUILD_DATE
ARG FULL_VERSION
RUN dotnet publish "./Amendment.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false \
    -p:VERSION="$VERSION" \
    -p:BUILD_NUMBER="$BUILD_NUMBER" \
    -p:BUILD_DATE="$BUILD_DATE" \
    -p:FULL_VERSION="$FULL_VERSION"

FROM base AS final
ARG VERSION=1.0.0
ARG BUILD_NUMBER=0
ARG BUILD_DATE
ARG FULL_VERSION

# Set environment variables for runtime access
ENV VERSION=$VERSION
ENV BUILD_NUMBER=$BUILD_NUMBER
ENV BUILD_DATE=$BUILD_DATE
ENV FULL_VERSION=$FULL_VERSION

WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Amendment.Server.dll"]