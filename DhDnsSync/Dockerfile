#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["DhDnsSync/DhDnsSync.csproj", "DhDnsSync/"]
RUN dotnet restore "DhDnsSync/DhDnsSync.csproj"
COPY . .
WORKDIR "/src/DhDnsSync"
RUN dotnet build "DhDnsSync.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DhDnsSync.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DhDnsSync.dll"]