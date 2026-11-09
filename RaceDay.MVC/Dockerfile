FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY RaceDay.Contracts/RaceDay.Contracts.csproj RaceDay.Contracts/
COPY RaceDay.MVC/RaceDay.MVC.csproj RaceDay.MVC/
RUN dotnet restore RaceDay.MVC/RaceDay.MVC.csproj
COPY RaceDay.Contracts/ RaceDay.Contracts/
COPY RaceDay.MVC/ RaceDay.MVC/
RUN dotnet publish RaceDay.MVC/RaceDay.MVC.csproj -c Release -o /out --no-restore /p:UseAppHost=false
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "RaceDay.MVC.dll"]
