FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["GovernmentCollections.API/GovernmentCollections.API.csproj", "GovernmentCollections.API/"]
COPY ["GovernmentCollections.Service/GovernmentCollections.Service.csproj", "GovernmentCollections.Service/"]
COPY ["GovernmentCollections.Data/GovernmentCollections.Data.csproj", "GovernmentCollections.Data/"]
COPY ["GovernmentCollections.Domain/GovernmentCollections.Domain.csproj", "GovernmentCollections.Domain/"]

RUN dotnet restore "GovernmentCollections.API/GovernmentCollections.API.csproj"
COPY . .
WORKDIR "/src/GovernmentCollections.API"
RUN dotnet build "GovernmentCollections.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GovernmentCollections.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GovernmentCollections.API.dll"]