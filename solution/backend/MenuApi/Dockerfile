# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and restore
COPY ../../MagiDesk.sln ./
COPY ../MenuApi/MenuApi.csproj ./MenuApi/MenuApi.csproj
RUN dotnet restore ./MenuApi/MenuApi.csproj

# Copy the rest and publish
COPY ../MenuApi/. ./MenuApi/
WORKDIR /src/MenuApi
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "MenuApi.dll"]
