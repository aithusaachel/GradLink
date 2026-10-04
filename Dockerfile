FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy solution and project files first
COPY src/GradLink.Shared/GradLink.Shared.csproj src/GradLink.Shared/
COPY src/GradLink.API/GradLink.API.csproj src/GradLink.API/
COPY src/GradLink.Client/GradLink.Client.csproj src/GradLink.Client/

# Restore dependencies
RUN dotnet restore src/GradLink.API/GradLink.API.csproj

# Copy everything else
COPY . .

# Build and publish the API (which will also build the Client due to ProjectReference)
WORKDIR /app/src/GradLink.API
RUN dotnet publish -c Release -o /app/publish

# Use the ASP.NET runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Expose ports used by Render
EXPOSE 8080

COPY --from=build /app/publish .

# Explicitly set urls so Render can bind to the port it expects
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "GradLink.API.dll"]
