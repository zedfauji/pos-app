FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["backend/DiscountApi/DiscountApi.csproj", "backend/DiscountApi/"]
RUN dotnet restore "backend/DiscountApi/DiscountApi.csproj"
COPY . .
WORKDIR "/src/backend/DiscountApi"
RUN dotnet build "DiscountApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DiscountApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DiscountApi.dll"]
