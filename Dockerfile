# Fernwood Heritage Retreat - Render deployment (.NET 8, ASP.NET Core MVC)
# Build stage: compiles the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY HotelBookingSystem.csproj ./
RUN dotnet restore
COPY . ./
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage: runs the published app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true
EXPOSE 8080
CMD sh -c "ASPNETCORE_URLS=http://+:${PORT:-10000} dotnet HotelBookingSystem.dll"
