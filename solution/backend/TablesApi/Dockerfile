## Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Expect build context at solution/
# Copy csproj for better caching and restore
COPY backend/TablesApi/*.csproj TablesApi/
COPY shared/*.csproj /shared/
RUN dotnet restore "TablesApi/TablesApi.csproj"

# Copy source and publish
COPY backend/TablesApi/ TablesApi/
COPY shared/ /shared/
RUN dotnet publish "TablesApi/TablesApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

## Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "TablesApi.dll"]
