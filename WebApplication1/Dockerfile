FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER root
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["WebApplication1.csproj", "./"]
RUN dotnet restore "WebApplication1.csproj"
COPY . .
RUN dotnet build "WebApplication1.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM --platform=linux/amd64 build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "WebApplication1.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false


FROM --platform=linux/amd64 base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN apt update && apt install -y ffmpeg
ENTRYPOINT ["dotnet", "WebApplication1.dll"]