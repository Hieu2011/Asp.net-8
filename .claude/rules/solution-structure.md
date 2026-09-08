# Cấu trúc solution hiện tại

Monorepo, 2 solution riêng biệt (`WebApiCore8.sln` — Business API, và `AuthService/AuthService.sln` — Auth/Identity, đang skeleton), **không có `ProjectReference` chéo giữa 2 solution** (ranh giới microservice — mỗi bên chỉ giao tiếp qua JWT/API sau này).

Cả 2 đều theo Clean Architecture: `Api → Infrastructure → Application → Domain`, một chiều duy nhất, `Domain` không phụ thuộc gì. Interface/port khai báo ở `Application`, implementation ở `Infrastructure`, đăng ký DI qua extension `AddApplicationServices()`/`AddInfrastructureServices()`, gọi gộp trong `Api/StartupConfig.cs`.
