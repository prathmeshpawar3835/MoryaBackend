FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY GramShopPOS.sln ./
COPY Directory.Build.props ./
COPY Backend/GramShopPOS.Domain/GramShopPOS.Domain.csproj Backend/GramShopPOS.Domain/
COPY Backend/GramShopPOS.Application/GramShopPOS.Application.csproj Backend/GramShopPOS.Application/
COPY Backend/GramShopPOS.Infrastructure/GramShopPOS.Infrastructure.csproj Backend/GramShopPOS.Infrastructure/
COPY Backend/GramShopPOS.API/GramShopPOS.API.csproj Backend/GramShopPOS.API/
RUN dotnet restore Backend/GramShopPOS.API/GramShopPOS.API.csproj
COPY Backend Backend
COPY Directory.Build.props ./
RUN dotnet publish Backend/GramShopPOS.API/GramShopPOS.API.csproj -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "GramShopPOS.API.dll"]
