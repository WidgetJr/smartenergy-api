FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["SmartEnergy.Api.csproj", "."]
RUN dotnet restore "SmartEnergy.Api.csproj"

COPY . .
RUN dotnet publish "SmartEnergy.Api.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "SmartEnergy.Api.dll"]
