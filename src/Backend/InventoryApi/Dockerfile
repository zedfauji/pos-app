# InventoryApi Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Build context is repo root (dockerContext: .)
COPY ./ ./

# Build and publish
WORKDIR /src/src/Backend/InventoryApi
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
# Copy config if it exists (optional)
COPY --from=build /src/src/Backend/config/serviceAccount.json ./config/serviceAccount.json 2>/dev/null || true
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "InventoryApi.dll"]
