# =======================
# State 1: Build
# =======================

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY AuthAPI.slnx .
COPY AuthAPI/AuthAPI.csproj AuthAPI/
COPY AuthAPI.Tests/AuthAPI.Tests.csproj AuthAPI.Tests/

# Restore dependencies
RUN dotnet restore

# Copy everything
COPY . .

# Build
RUN dotnet build -c Release --no-restore

# Run tests
RUN dotnet test -c Release --no-build --verbosity normal

# =======================
# State 2: Publish
# =======================

FROM build AS publish
RUN dotnet publish AuthAPI/AuthAPI.csproj -c Release -o /app/publish --no-build

# =======================
# State 3: Runtime
# =======================

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Copy published files
COPY --from=publish /app/publish .

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Run
ENTRYPOINT ["dotnet", "AuthAPI.dll"]
