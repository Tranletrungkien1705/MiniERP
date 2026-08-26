FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/MiniERP.Api/MiniERP.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
# Render free: tắt reloadConfigOnChange để tránh crash inotify watcher limit
ENV DOTNET_hostBuilder__reloadConfigOnChange=false
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["dotnet", "MiniERP.Api.dll"]
