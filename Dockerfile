FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

COPY src/DmAdminApi/DmAdminApi.csproj ./src/DmAdminApi/
RUN dotnet restore src/DmAdminApi/DmAdminApi.csproj

COPY src/DmAdminApi/ ./src/DmAdminApi/
RUN dotnet publish src/DmAdminApi/DmAdminApi.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /out .
EXPOSE 8080
ENTRYPOINT ["dotnet", "DmAdminApi.dll"]
