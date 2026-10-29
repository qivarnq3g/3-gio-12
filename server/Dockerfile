FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/ThreeTwelve.Api/ThreeTwelve.Api.csproj src/ThreeTwelve.Api/
RUN dotnet restore src/ThreeTwelve.Api/ThreeTwelve.Api.csproj
COPY src/ src/
RUN dotnet publish src/ThreeTwelve.Api/ThreeTwelve.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "ThreeTwelve.Api.dll"]
