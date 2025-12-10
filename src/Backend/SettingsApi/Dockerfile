## Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Build context is repo root (dockerContext: .)
COPY ./ ./

# Build and publish
WORKDIR /src/src/Backend/SettingsApi
RUN dotnet publish -c Release -o /app/publish

## Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy published app
COPY --from=build /app/publish .

# Copy Firestore service account into container (if present)
COPY --from=build /src/src/Backend/config/serviceAccount.json /secrets/serviceAccount.json 2>/dev/null || true
ENV GOOGLE_APPLICATION_CREDENTIALS=/secrets/serviceAccount.json

# Expose port for Cloud Run
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SettingsApi.dll"]
