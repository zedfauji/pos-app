## Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Expect build context at solution/ so we can see backend/ and shared/
COPY ["shared/MagiDesk.Shared.csproj", "shared/"]
COPY ["backend/SettingsApi/SettingsApi.csproj", "backend/SettingsApi/"]
RUN dotnet restore "backend/SettingsApi/SettingsApi.csproj"

# Copy source and publish
COPY shared/ shared/
COPY backend/SettingsApi/ backend/SettingsApi/
WORKDIR /src/backend/SettingsApi
RUN dotnet publish "SettingsApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

## Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy published app
COPY --from=build /app/publish .

# Copy Firestore service account into container (if present)
COPY backend/config/serviceAccount.json /secrets/serviceAccount.json
ENV GOOGLE_APPLICATION_CREDENTIALS=/secrets/serviceAccount.json

# Expose port for Cloud Run
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SettingsApi.dll"]
