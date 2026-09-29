# =========================
# BUILD
# =========================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Copy toàn bộ source code
COPY . .

# Restore dependencies
RUN dotnet restore src/HungStore.API/HungStore.API.csproj

# Build + publish API
RUN dotnet publish src/HungStore.API/HungStore.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore


# =========================
# RUN
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

# Copy file đã publish từ build stage
COPY --from=build /app/publish .

# ASP.NET Core listen trên port 8000
ENV ASPNETCORE_URLS=http://+:8000

EXPOSE 8000

# Chạy API
ENTRYPOINT ["dotnet", "HungStore.API.dll"]