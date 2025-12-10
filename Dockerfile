## This is a placeholder Dockerfile for DigitalOcean auto-detection
## DigitalOcean needs to detect SOMETHING to proceed past the "No components detected" error
## Once past this screen, you'll manually configure all 9 services with their individual source directories

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# This file exists only to bypass DO's detection blocker
# Your actual services will be configured manually with source_dir: src/Backend/ServiceName
COPY ./src/Backend/TablesApi/ ./TablesApi/

WORKDIR /src/TablesApi
RUN dotnet publish -c Release -o /app/publish

## Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "TablesApi.dll"]

