# Multi-stage build for Granite.Server
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files for restore layer caching
COPY GraniteServer.sln ./
COPY Granite.Common/Granite.Common.csproj Granite.Common/
COPY Granite.Data/Granite.Data.csproj Granite.Data/
COPY Granite.Mod/Granite.Mod.csproj Granite.Mod/
COPY Granite.Server/Granite.Server.csproj Granite.Server/
COPY Granite.Web.Client/Granite.Web.Client.csproj Granite.Web.Client/

RUN dotnet restore GraniteServer.sln

# Copy the rest of the source
COPY . .

# Publish the Blazor WebAssembly client
RUN dotnet publish -c Release Granite.Web.Client/Granite.Web.Client.csproj -o /app/client

# Publish the server with Blazor assets
WORKDIR /src/Granite.Server
RUN dotnet publish -c Release -o /app/publish /p:BuildWebApp=true

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Granite.Server.dll"]
