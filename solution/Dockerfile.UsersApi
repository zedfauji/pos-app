# Build from the solution root to access shared projects
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files for restore
COPY ["backend/UsersApi/UsersApi.csproj", "backend/UsersApi/"]
COPY ["shared/MagiDesk.Shared.csproj", "shared/"]
RUN dotnet restore "backend/UsersApi/UsersApi.csproj"

# Copy source code
COPY backend/UsersApi/ backend/UsersApi/
COPY shared/ shared/

# Build and publish
WORKDIR /src/backend/UsersApi
RUN dotnet publish "UsersApi.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "UsersApi.dll"]
