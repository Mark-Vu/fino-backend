# Runtime
FROM mcr.microsoft.com/dotnet/nightly/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Build
FROM mcr.microsoft.com/dotnet/nightly/sdk:10.0 AS build
WORKDIR /src

# copy csproj first for restore caching
COPY FinoBackend.csproj ./
RUN dotnet restore "FinoBackend.csproj"

# copy the rest and publish
COPY . .
RUN dotnet publish "FinoBackend.csproj" -c Release -o /app/publish /p:UseAppHost=false

# final
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet","FinoBackend.dll"]
