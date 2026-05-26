FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["CustomerService/CustomerService.csproj", "CustomerService/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "CustomerService/CustomerService.csproj"

COPY . .
WORKDIR "/src/CustomerService"
RUN dotnet build "CustomerService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CustomerService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CustomerService.dll"]
