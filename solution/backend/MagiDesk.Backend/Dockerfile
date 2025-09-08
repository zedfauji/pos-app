## Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Expect build context at solution/ so we can see backend/ and shared/
# Copy csproj and restore as distinct layers for better caching
COPY backend/MagiDesk.Backend/*.csproj MagiDesk.Backend/
COPY shared/*.csproj /shared/
RUN dotnet restore "MagiDesk.Backend/MagiDesk.Backend.csproj"

# Copy source and publish
COPY backend/MagiDesk.Backend/ MagiDesk.Backend/
COPY shared/ /shared/
RUN dotnet publish "MagiDesk.Backend/MagiDesk.Backend.csproj" -c Release -o /app/publish /p:UseAppHost=false

## Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy published app
COPY --from=build /app/publish .

# Copy Firestore service account from backend/config into container (if present)
# If you prefer Secret Manager, omit this and mount as a secret at deploy time
COPY backend/config/serviceAccount.json /secrets/serviceAccount.json
ENV GOOGLE_APPLICATION_CREDENTIALS=/secrets/serviceAccount.json

# Expose port for Cloud Run
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "MagiDesk.Backend.dll"]
