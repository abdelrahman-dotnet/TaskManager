FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

COPY . .

RUN dotnet restore "TaskManager.API/TaskManager.API.csproj"
RUN dotnet publish "TaskManager.API/TaskManager.API.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:6.0

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:10000

EXPOSE 10000

ENTRYPOINT ["dotnet", "TaskManager.API.dll"]