# InventoryApi Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY backend/InventoryApi/*.csproj InventoryApi/
COPY shared/*.csproj /shared/

RUN dotnet restore "InventoryApi/InventoryApi.csproj"

# Copy source
COPY backend/InventoryApi/ InventoryApi/
COPY shared/ /shared/

RUN dotnet publish "InventoryApi/InventoryApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY backend/config/serviceAccount.json ./config/serviceAccount.json
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "InventoryApi.dll"]
