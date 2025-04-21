FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["SuperHeroAPI/SuperHeroAPI.csproj", "SuperHeroAPI/"]
RUN dotnet restore "SuperHeroAPI/SuperHeroAPI.csproj"
COPY . .
WORKDIR "/src/SuperHeroAPI"
RUN dotnet build "SuperHeroAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SuperHeroAPI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Copy the creation script directly to the app directory
COPY SuperHeroAPI/creationscript.txt /app/
ENTRYPOINT ["dotnet", "SuperHeroAPI.dll"] 