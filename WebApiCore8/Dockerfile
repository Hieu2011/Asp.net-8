# Base image cho runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Image để build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Sao chép Solution và các dự án vào container
COPY ["WebApiCore8.sln", "."]
COPY ["ApiCore8/ApiCore8.csproj", "ApiCore8/"]
COPY ["Core/Core.csproj", "Core/"]
COPY ["ML/ML.csproj", "ML/"]
COPY ["DAL/DAL.csproj", "DAL/"]
COPY ["BLL/BLL.csproj", "BLL/"]

# Restore các package của solution
RUN dotnet restore "WebApiCore8.sln"

# Sao chép các file còn lại
COPY . .

# Build ứng dụng
RUN dotnet build "WebApiCore8.sln" -c $BUILD_CONFIGURATION -o /app/build

# Image để publish
FROM build AS publish
RUN dotnet publish "WebApiCore8.sln" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Image cuối cùng cho runtime
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ApiCore8.dll"]
