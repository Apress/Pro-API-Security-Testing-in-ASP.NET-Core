FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
# ENV ASPNETCORE_HTTP_PORTS=5000
WORKDIR /app
EXPOSE 5000
# EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["VulnerableBankApi.csproj", "./"]
RUN dotnet restore "VulnerableBankApi.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "VulnerableBankApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "VulnerableBankApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "VulnerableBankApi.dll"]