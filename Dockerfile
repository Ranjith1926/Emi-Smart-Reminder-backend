FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY EMI-REMAINDER/EMI-REMAINDER.csproj EMI-REMAINDER/
RUN dotnet restore EMI-REMAINDER/EMI-REMAINDER.csproj

COPY EMI-REMAINDER/ EMI-REMAINDER/
RUN dotnet publish EMI-REMAINDER/EMI-REMAINDER.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "EMI-REMAINDER.dll"]
