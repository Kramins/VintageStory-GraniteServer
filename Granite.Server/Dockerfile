# Multi-stage build for Granite.Server
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Install Node.js for frontend build
RUN apt-get update && apt-get install -y --no-install-recommends nodejs npm && rm -rf /var/lib/apt/lists/*

# Copy solution and project files for restore layer caching
COPY GraniteServer.sln ./
COPY Granite.Common/Granite.Common.csproj Granite.Common/
COPY Granite.Data/Granite.Data.csproj Granite.Data/
COPY Granite.Mod/Granite.Mod.csproj Granite.Mod/
COPY Granite.Server/Granite.Server.csproj Granite.Server/

RUN dotnet restore GraniteServer.sln

# Copy the rest of the source
COPY . .

# Build the frontend
WORKDIR /src/Granite.Server/ClientApp
RUN npm ci --silent
RUN npm run build

# Publish the server with SPA assets
WORKDIR /src/Granite.Server
RUN dotnet publish -c Release -o /app/publish /p:BuildWebApp=true

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Granite.Server.dll"]
