FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["InventoryService/InventoryService.csproj", "InventoryService/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "InventoryService/InventoryService.csproj"

COPY . .
WORKDIR "/src/InventoryService"
RUN dotnet build "InventoryService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "InventoryService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "InventoryService.dll"]
