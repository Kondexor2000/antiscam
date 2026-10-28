FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/AntiScam.Blog.Api/AntiScam.Blog.Api.csproj src/AntiScam.Blog.Api/
RUN dotnet restore src/AntiScam.Blog.Api/AntiScam.Blog.Api.csproj

COPY src/AntiScam.Blog.Api/ src/AntiScam.Blog.Api/
RUN dotnet publish src/AntiScam.Blog.Api/AntiScam.Blog.Api.csproj \
    --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "AntiScam.Blog.Api.dll"]
