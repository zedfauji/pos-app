# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# The deploy script copies this Dockerfile to the solution root.
# Copy entire solution to ensure all props/central packages are present
COPY ./ ./

# Publish
# Note: Build context is repo root, so COPY ./ ./ creates /src/src/Backend/ structure
WORKDIR /src/src/Backend/OrderApi
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "OrderApi.dll"]
