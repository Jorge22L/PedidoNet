FROM mcr.microsoft.com/dotnet/aspnet:10.0 as runtime
WORKDIR /app

EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 as build
WORKDIR /src

COPY ["src/Api/Api.csproj", "Api/"]
COPY ["src/Application/Application.csproj", "Application/"]
COPY ["src/Domain/Domain.csproj", "Domain/"]
COPY ["src/Infrastructure/Infrastructure.csproj", "Infrastructure/"]
COPY ["src/Persistence/Persistence.csproj", "Persistence/"]
COPY ["src/Middleware/Middleware.csproj", "Middleware/"]

RUN dotnet restore "Api/Api.csproj"

COPY src/ .

WORKDIR "/src/Api"

RUN dotnet publish "Api.csproj" -c Release -o /app/publish --no-restore

FROM runtime as final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Api.dll"]