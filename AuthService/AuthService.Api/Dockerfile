# Bước 1: Sử dụng hình ảnh SDK để xây dựng ứng dụng
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Bước 2: Sao chép tất cả các file .csproj và khôi phục các phụ thuộc
COPY ["AuthService.Api/AuthService.Api.csproj", "AuthService.Api/"]
COPY ["AuthService.Application/AuthService.Application.csproj", "AuthService.Application/"]
COPY ["AuthService.Infrastructure/AuthService.Infrastructure.csproj", "AuthService.Infrastructure/"]
COPY ["AuthService.Domain/AuthService.Domain.csproj", "AuthService.Domain/"]

RUN dotnet restore "AuthService.Api/AuthService.Api.csproj"

# Bước 3: Sao chép toàn bộ mã nguồn và xây dựng ứng dụng
COPY . .
RUN dotnet build "AuthService.Api/AuthService.Api.csproj" -c Release -o /app/build

# Bước 4: Xuất bản ứng dụng
FROM build AS publish
RUN dotnet publish "AuthService.Api/AuthService.Api.csproj" -c Release -o /app/publish

# Bước 5: Tạo hình ảnh cuối cùng chỉ với ứng dụng đã xuất bản
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish ./
ENTRYPOINT ["dotnet", "AuthService.Api.dll"]
