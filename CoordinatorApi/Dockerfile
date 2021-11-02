#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
EXPOSE 8282
EXPOSE 11111
EXPOSE 30000

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["CoordinatorApi/CoordinatorApi.csproj", "CoordinatorApi/"]
COPY ["Events/Events.csproj", "Events/"]
COPY ["SharedMiddleware/SharedMiddleware.csproj", "SharedMiddleware/"]
RUN dotnet restore "CoordinatorApi/CoordinatorApi.csproj"
COPY . .
WORKDIR "/src/CoordinatorApi"
RUN dotnet build "CoordinatorApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CoordinatorApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CoordinatorApi.dll"]
