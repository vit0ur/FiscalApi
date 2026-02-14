FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . ../
WORKDIR /src
RUN dotnet restore ../FiscalDocumentProcessor.sln
RUN dotnet publish ../FiscalDocumentProcessor.Worker/FiscalDocumentProcessor.Worker.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FiscalDocumentProcessor.Worker.dll"]
