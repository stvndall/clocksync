FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["clockSync/clockSync.csproj", "clockSync/"]
RUN dotnet restore "clockSync/clockSync.csproj"
COPY . .
WORKDIR "/src/clockSync"
RUN dotnet build "clockSync.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "clockSync.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "clockSync.dll"]
