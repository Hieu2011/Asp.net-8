# Bước 1: Sử dụng hình ảnh SDK để xây dựng ứng dụng
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Bước 2: Sao chép tất cả các file .csproj và khôi phục các phụ thuộc
COPY ["ApiCore8/ApiCore8.csproj", "ApiCore8/"]
COPY ["BLL/BLL.csproj", "BLL/"]
COPY ["DAL/DAL.csproj", "DAL/"]
COPY ["ML/ML.csproj", "ML/"]
COPY ["Core/Core.csproj", "Core/"]

RUN dotnet restore "ApiCore8/ApiCore8.csproj"

# Bước 3: Sao chép toàn bộ mã nguồn và xây dựng ứng dụng
COPY . .
RUN dotnet build "ApiCore8/ApiCore8.csproj" -c Release -o /app/build

# Bước 4: Xuất bản ứng dụng
FROM build AS publish
RUN dotnet publish "ApiCore8/ApiCore8.csproj" -c Release -o /app/publish

# Bước 5: Tạo hình ảnh cuối cùng chỉ với ứng dụng đã xuất bản
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish ./
ENTRYPOINT ["dotnet", "ApiCore8.dll"]
