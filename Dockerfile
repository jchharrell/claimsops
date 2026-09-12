FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/ClaimsOps.Api/ClaimsOps.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENV CLAIMSOPS_DB=/data/claimsops.db
VOLUME ["/data"]
EXPOSE 8080
ENTRYPOINT ["dotnet", "ClaimsOps.Api.dll"]
