#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 20222

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Server/AtcDemo.Server.csproj", "Server/"]
COPY ["Client/AtcDemo.Client.csproj", "Client/"]
COPY ["QuickGrid/QuickGrid.csproj", "QuickGrid/"]
COPY ["Shared/AtcDemo.Shared.csproj", "Shared/"]
COPY ["AtcDemo.sln", "/"]
COPY ["Directory.Build.props", "/"]
RUN dotnet restore "Server/AtcDemo.Server.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "AtcDemo.sln" -c Release -o /app/build

WORKDIR "/src/Server"
FROM build AS publish
RUN dotnet publish "AtcDemo.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AtcDemo.Server.dll"]
