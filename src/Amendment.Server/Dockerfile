#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
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
RUN dotnet build "./Amendment.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Amendment.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Amendment.Server.dll"]