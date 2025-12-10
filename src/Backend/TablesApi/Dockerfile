## Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Build context is repo root (dockerContext: .)
# Copy entire solution to ensure Directory.Build.props is available
COPY ./ ./

# Build and publish
WORKDIR /src/src/Backend/TablesApi
RUN dotnet publish -c Release -o /app/publish

## Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "TablesApi.dll"]
