# Stage 1: Build and publish the app
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ./AuthService.csproj ./ 
RUN dotnet restore ./AuthService.csproj

# Copy the rest of the source code
COPY . .

# Publish the app to /app/publish
RUN dotnet publish ./AuthService.csproj -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Copy published files from build stage
COPY --from=build /app/publish .

# Render sets PORT environment variable automatically
ENV ASPNETCORE_URLS=http://+:${PORT}

# Expose the port (optional, Render handles it)
EXPOSE 8080

# Run the app
ENTRYPOINT ["dotnet", "AuthService.dll"]