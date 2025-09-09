# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# The deploy script copies this Dockerfile to the solution root.
# Paths below assume build context is the solution root.

# Copy entire solution to ensure Directory.Packages.props/props are available for restore
COPY ./ ./

# Copy the rest and publish
WORKDIR /src/backend/MenuApi
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "MenuApi.dll"]
