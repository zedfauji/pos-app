# Build from the solution root to access shared projects
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Build context is repo root (dockerContext: .)
# Copy entire solution to ensure Directory.Build.props is available
COPY ./ ./

# Build and publish
WORKDIR /src/src/Backend/UsersApi
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "UsersApi.dll"]
