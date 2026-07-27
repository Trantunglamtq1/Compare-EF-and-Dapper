FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["CompareEfAndDapper.Web/CompareEfAndDapper.Web.csproj", "CompareEfAndDapper.Web/"]
RUN dotnet restore "CompareEfAndDapper.Web/CompareEfAndDapper.Web.csproj"

COPY . .
WORKDIR "/src/CompareEfAndDapper.Web"
RUN dotnet build "CompareEfAndDapper.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CompareEfAndDapper.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CompareEfAndDapper.Web.dll"]
