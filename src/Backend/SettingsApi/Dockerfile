## Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Expect build context at solution/ so we can see backend/ and shared/
COPY src/Shared/shared/*.csproj shared/
COPY src/Backend/SettingsApi/*.csproj SettingsApi/
RUN dotnet restore "SettingsApi/SettingsApi.csproj"

# Copy source and publish
COPY src/Shared/shared/ shared/
COPY src/Backend/SettingsApi/ SettingsApi/
RUN dotnet publish "SettingsApi/SettingsApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

## Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy published app
COPY --from=build /app/publish .

# Copy Firestore service account into container (if present)
COPY src/Backend/config/serviceAccount.json /secrets/serviceAccount.json
ENV GOOGLE_APPLICATION_CREDENTIALS=/secrets/serviceAccount.json

# Expose port for Cloud Run
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SettingsApi.dll"]
