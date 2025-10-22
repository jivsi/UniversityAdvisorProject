FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY UniversityAdvisor/UniversityAdvisor.csproj UniversityAdvisor/
RUN dotnet restore UniversityAdvisor/UniversityAdvisor.csproj

COPY UniversityAdvisor/ UniversityAdvisor/
WORKDIR /src/UniversityAdvisor
RUN dotnet build UniversityAdvisor.csproj -c Release -o /app/build

FROM build AS publish
RUN dotnet publish UniversityAdvisor.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080

COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "UniversityAdvisor.dll"]
