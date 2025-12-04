FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/KeyLoyalty.API/KeyLoyalty.API.csproj", "src/KeyLoyalty.API/"]
COPY ["src/KeyLoyalty.Application/KeyLoyalty.Application.csproj", "src/KeyLoyalty.Application/"]
COPY ["src/KeyLoyalty.Domain/KeyLoyalty.Domain.csproj", "src/KeyLoyalty.Domain/"]
COPY ["src/KeyLoyalty.Infrastructure/KeyLoyalty.Infrastructure.csproj", "src/KeyLoyalty.Infrastructure/"]
RUN dotnet restore "src/KeyLoyalty.API/KeyLoyalty.API.csproj"
COPY . .
WORKDIR "/src/src/KeyLoyalty.API"
RUN dotnet build "KeyLoyalty.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "KeyLoyalty.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "KeyLoyalty.API.dll"]