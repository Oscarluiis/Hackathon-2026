FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Hackathon-Docente-2026.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish Hackathon-Docente-2026.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Hackathon-Docente-2026.dll"]