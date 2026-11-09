FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY RaceDay.Contracts/RaceDay.Contracts.csproj RaceDay.Contracts/
COPY RaceDay.Api/RaceDay.Api.csproj RaceDay.Api/
RUN dotnet restore RaceDay.Api/RaceDay.Api.csproj
COPY RaceDay.Contracts/ RaceDay.Contracts/
COPY RaceDay.Api/ RaceDay.Api/
RUN dotnet publish RaceDay.Api/RaceDay.Api.csproj -c Release -o /out --no-restore /p:UseAppHost=false
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "RaceDay.Api.dll"]
