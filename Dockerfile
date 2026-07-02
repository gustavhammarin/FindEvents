FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY App/App.csproj App/
RUN dotnet restore App/App.csproj
COPY App/ App/
RUN dotnet publish App/App.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["dotnet", "App.dll"]
