# InventoryApi Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files for restore
COPY ["backend/InventoryApi/InventoryApi.csproj", "backend/InventoryApi/"]
COPY ["shared/MagiDesk.Shared.csproj", "shared/"]
RUN dotnet restore "backend/InventoryApi/InventoryApi.csproj"

# Copy source code
COPY backend/InventoryApi/ backend/InventoryApi/
COPY shared/ shared/

# Build and publish
WORKDIR /src/backend/InventoryApi
RUN dotnet publish "InventoryApi.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY backend/config/serviceAccount.json ./config/serviceAccount.json
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "InventoryApi.dll"]
