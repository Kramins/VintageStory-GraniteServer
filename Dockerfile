# Build stage - uses published artifacts from Cake build
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

# Expose ports for Kestrel
EXPOSE 80
EXPOSE 443

# Copy published application artifacts
FROM base AS final
COPY ./Granite.Server/obj/publish/ ./

# Set environment variables
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

# Run the application
ENTRYPOINT ["dotnet", "Granite.Server.dll"]
