#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["AttributesConfigServiceMock/AttributesConfigServiceMock.csproj", "AttributesConfigServiceMock/"]
COPY ["Events/Events.csproj", "Events/"]
COPY ["SharedMiddleware/SharedMiddleware.csproj", "SharedMiddleware/"]
RUN dotnet restore "AttributesConfigServiceMock/AttributesConfigServiceMock.csproj"
COPY . .
WORKDIR "/src/AttributesConfigServiceMock"
RUN dotnet build "AttributesConfigServiceMock.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AttributesConfigServiceMock.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AttributesConfigServiceMock.dll"]
