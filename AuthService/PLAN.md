# Auth Service — Plan triển khai (chờ duyệt trước khi code)

> Trạng thái: **DRAFT — chưa code gì, chờ anh duyệt**. Sau khi duyệt, code theo đúng thứ tự ở mục "Thứ tự code", có checkpoint review từng bước, không code 1 lèo.

## 1. Quyết định đã chốt

| # | Hạng mục | Quyết định |
|---|---|---|
| 1 | Password hashing | `BCrypt.Net-Next` (không dùng `PasswordHasher<TUser>`/Identity) |
| 2 | QR code đăng ký TOTP | Chỉ trả chuỗi `otpauth://totp/...`, không render ảnh QR (không cần `QRCoder`) — anh tự dùng tool online hoặc nhập tay secret vào Google Authenticator lúc test |
| 3 | TOTP | `Otp.NET` |
| 4 | JWT | `Microsoft.AspNetCore.Authentication.JwtBearer` + `System.IdentityModel.Tokens.Jwt`, ký bằng RSA (tự ký, không Keycloak/OpenIddict) |
| 5 | Data access | Raw ADO.NET qua **stored function Postgres** (`sp_*` + `refcursor`), đúng pattern đang dùng ở `scripts/postgres_users_setup.sql` — không EF Core, không Identity |
| 6 | RBAC | 5 cấp bậc theo số (số nhỏ = quyền cao) |
| 7 | `/auth/register` công khai | Luôn tạo user cấp 5 (Khách hàng) |
| 8 | Bootstrap Admin đầu tiên | Seed thẳng bằng SQL script lúc setup DB (không qua API — trứng-gà problem, không ai cấp cao hơn để tạo Admin đầu tiên) |
| 9 | Xóa user | **Soft delete** (`is_deleted`, `deleted_date`, `deleted_user`) — giữ audit trail, không xóa thật |
| 10 | Audit columns | Áp dụng cho mọi bảng (`users`, `sessions`) — xem mục 4 |
| 11 | Access token TTL | **5 phút** — sống ngắn để giới hạn thiệt hại nếu bị lộ |
| 12 | Kiểm tra token mỗi request | Business API (`ApiCore8.Api`) verify chữ ký JWT (RSA public key) **+ tra revocation qua Redis dùng chung** (`session_id` claim) — cho phép thu hồi có hiệu lực gần như ngay lập tức mà không cần gọi đồng bộ sang Auth Service mỗi request |
| 13 | Mã hóa `TotpSecret` | AES, key lấy từ config (`Auth:TotpEncryptionKey`) — **tái dùng đúng pattern đã có** ở `RedisConnectionService.CompressAndEncrypt/DecompressAndDecrypt` (SHA256-derive key, IV prepend, base64). User Secrets lúc dev, biến môi trường Coolify lúc go-live |
| 14 | Phạm vi quản lý user | **Chỉ Admin (cấp 1)** được đổi role/disable/xóa/thu hồi session của user khác — bỏ hẳn `CanManageUser` theo bậc thang tương đối, thay bằng check cứng `actor.RoleLevel == Admin`. Chặn thêm actor thao tác lên chính mình (`actor.Id == target.Id` → từ chối) |
| 15 | JWT — chặn algorithm confusion | Validate JWT ở `ApiCore8.Api` phải **ép cứng `ValidAlgorithms = ["RS256"]`**, từ chối mọi thuật toán khác (kể cả `none`/`HS256` dùng chính public key làm secret) |
| 16 | Kích thước RSA key | Tối thiểu **2048-bit**, khuyến nghị **3072-bit** |
| 17 | Khóa OTP sau 3 lần sai | Khóa **riêng đường OTP** trong thời gian cấu hình được (`Auth:OtpLockoutDuration`, mặc định 1 tiếng) — trong lúc khóa, dù nhập đúng OTP vẫn từ chối. **Password fallback vẫn dùng được ngay**, không cần chờ hết khóa. |
| 18 | Khóa toàn bộ tài khoản | Nếu Password fallback CŨNG sai quá ngưỡng (mặc định 3 lần, cùng cơ chế Redis) → khóa **toàn bộ tài khoản** (cả OTP lẫn Password) trong thời gian cấu hình được (`Auth:AccountLockoutDuration`, mặc định 1 tiếng) |
| 19 | Admin mở khóa | Admin mở khóa OTP-lock/account-lock cho user bất kỳ lúc nào (endpoint riêng, chỉ Admin) — xóa lockout key trong Redis + reset fail counter |
| 20 | Refresh Token Rotation + Reuse Detection | Mỗi lần refresh: **UPDATE 2 cột trên cùng dòng `Session`** (`refresh_token_hash` mới, `previous_refresh_token_hash` = giá trị cũ) — **không tạo dòng mới**, không phình bảng dù refresh liên tục. Nếu client gửi lại `previous_refresh_token_hash` (token đã bị thay) → nghi bị đánh cắp → revoke toàn bộ session ngay |
| 21 | `Session.ExpiresAt` là giới hạn tuyệt đối | Học từ Keycloak "SSO Session Max" — refresh token không được gia hạn vượt quá mốc này dù rotation liên tục |
| 22 | Đối tác ngoài (Flow B) | Dùng **OAuth2 Client Credentials Grant** với `client_secret` (Basic Auth header, chuẩn RFC 6749 + Keycloak) — **bỏ hẳn `public_key`/`private_key_jwt`** (quá phức tạp cho đối tác, không cần thiết với quy mô hiện tại). Không có OAuth3/OAuth4 — OAuth2 vẫn là chuẩn M2M phổ biến nhất 2026 |
| 23 | IP allowlist đối tác | Implement **trong code AuthService** (cột `allowed_ips` trong bảng `clients`), không dùng Traefik custom label (có bug đã biết ở Coolify — issue #2196/#2818) |
| 24 | Lấy IP client thật | Ưu tiên header `CF-Connecting-IP` → `X-Forwarded-For` → fallback `RemoteIpAddress`. **Chỉ tin header khi `RemoteIpAddress` nằm trong dải IP CHÍNH THỨC của Cloudflare** (`cloudflare.com/ips`) — chặn tấn công giả mạo header khi có ai bypass được Cloudflare (VD từ LAN nội bộ) |
| 25 | Phòng thủ cho `client_secret` bị lộ | Rate limit + audit log (IP, thời gian) theo từng `client_id`; 2 endpoint Admin: **regenerate secret** (vô hiệu ngay, không grace period) và **disable client** (khóa khẩn cấp) |
| 26 | Tương lai — nếu deploy VPS có IP public | Chuyển Cloudflare Tunnel → **Cloudflare Proxy** (DNS "Proxied", đám mây cam) + **firewall VPS chỉ nhận 80/443 từ dải IP Cloudflare** (khôi phục tính chất "không bypass được" như Tunnel hiện tại) |
| 27 | Password policy | 8–12 ký tự, bắt buộc ≥1 chữ hoa + ≥1 ký tự đặc biệt — validate ở Application layer (regex), lỗi trả rõ ràng, không chỉ dựa DB constraint |
| 28 | Username | UNIQUE (đã có ở schema mục 4b) — xác nhận lại, giữ nguyên |
| 29 | `/auth/login/password` phải có gate server-side | Chỉ chấp nhận khi `otp_fail_count:{userId} >= 3` hoặc user đang trong `otp_locked_until` (Redis) — không đủ điều kiện thì từ chối, bắt quay lại `/auth/login`. **Không có gate này thì OTP-first bị bypass hoàn toàn** |
| 30 | Chống replay OTP trong cùng 1 chu kỳ 30s | Sau khi verify OTP thành công, lưu `last_used_otp_step:{userId}` (Redis, TTL ~60s) — step trùng lần trước thì từ chối dù mã đúng (chặn network-replay/MITM) |
| 31 | Refresh token hash | **SHA256** (không dùng BCrypt) — refresh token đã random entropy cao sẵn, BCrypt chỉ tốn CPU vô ích mỗi lần refresh; BCrypt giữ riêng cho password/client_secret |
| 32 | `ClockSkew` khi validate JWT ở `ApiCore8.Api` | Set tường minh `TimeSpan.FromSeconds(30)` — mặc định `JwtBearer` là 5 phút, trùng khít TTL access token (5 phút) → vô hiệu hóa một nửa lý do TTL ngắn (mục 11) |
| 33 | Tự thu hồi TẤT CẢ session của chính mình | Thêm `DELETE /auth/sessions` (self, all) — ngoài thu hồi từng session lẻ đã có |
| 34 | Đổi role user khác cũng revoke session | `PUT /auth/users/{id}/role` tự revoke hết session hiện có của user đó trong cùng transaction — JWT cũ có thể còn mang `role_level` cũ tối đa 5 phút, revoke session ép logout ngay lập tức |
| 35 | Khóa vĩnh viễn sau khi hết hạn khóa tạm mà vẫn sai password | Đếm số lần bị khóa tạm bằng `account_lockout_count:{userId}` (Redis) — **từ lần khóa tạm thứ 2 trở đi** (tức hết `account_locked_until` mà password vẫn sai tiếp) → set `users.is_permanently_locked = true` (cột Postgres thật, không chỉ Redis, để Admin thấy được và không mất khi Redis bị flush) |
| 36 | Luồng tự phục hồi — "Quên mật khẩu" / "Tài khoản bị khóa" | Dùng **chung 1 cơ chế backend**, client chỉ hiển thị 2 nhãn riêng (Quên mật khẩu / Cấp lại quyền truy cập) cùng gọi chung API: gửi mã OTP 6 số **qua email đã đăng ký** → verify đúng mã → đổi password mới → backend tự động: xóa `otp_fail_count`/`password_fail_count`/`account_locked_until`/`is_permanently_locked` + sinh `TotpSecret` mới, trả `otpauthUri` mới (giống response `/auth/register`) — user quét lại QR, lần `/auth/login` kế tiếp bằng OTP mới chính là bước xác nhận enrollment, không cần endpoint "confirm" riêng |
| 37 | Admin vẫn là phương án dự phòng cuối | `PUT /auth/users/{id}/unlock` (mục 19) mở rộng: ngoài xóa khóa tạm, xóa luôn được `is_permanently_locked` — dùng khi user không còn truy cập được email đã đăng ký (liên hệ Admin trực tiếp ngoài hệ thống) |
| 38 | Gửi email | Thêm cột `email` (UNIQUE, NOT NULL) vào bảng `users`, bắt buộc nhập lúc `/auth/register`. Thư viện gửi mail đề xuất **`MailKit`** (thay `System.Net.Mail.SmtpClient` đã lỗi thời) — **hỏi anh trước khi cài**. Cấu hình SMTP qua User Secrets (`Smtp:Host/Port/Username/Password/From`), đúng convention secret hiện có |
| 39 | Chống dò username | Response của `/auth/login`, `/auth/login/password`, `/auth/password/forgot` không phân biệt "username không tồn tại" vs "sai OTP/password" — luôn trả cùng 1 thông báo chung chung. **Ngoại lệ có chủ đích:** trạng thái khóa (mục 40) được tiết lộ rõ ràng khi username tồn tại, để điều hướng user sang luồng cấp lại quyền truy cập — đánh đổi chấp nhận được vì chỉ lộ thêm "tài khoản này từng bị khóa", không lộ đúng/sai OTP hay password |
| 40 | Báo lỗi khóa tài khoản ngay khi đăng nhập | `/auth/login` và `/auth/login/password` check trạng thái khóa **TRƯỚC KHI** verify OTP/password, nhưng **CHỈ SAU KHI đã xác nhận username tồn tại** — username không tồn tại phải trả đúng y hệt lỗi generic `InvalidCredential` như sai OTP/password thường (không rẽ nhánh khác, tránh tạo oracle dò username mới). Nếu username tồn tại: `is_permanently_locked=true` → trả lỗi riêng `ACCOUNT_PERMANENTLY_LOCKED` (client hiện thông báo "Tài khoản đã bị khóa" + nút **"Cấp lại quyền truy cập"** trỏ thẳng luồng mục 36); đang `otp_locked_until`/`account_locked_until` còn hiệu lực → trả `OTP_LOCKED`/`ACCOUNT_LOCKED` kèm thời gian còn lại (client hiện đếm ngược, KHÔNG hiện nút cấp lại quyền vì tự hết khóa). Dùng enum `AuthErrorCode` trong `APIResult.StatusID` để client phân biệt các case này |
| 41 | Chặn brute-force mã reset password | `/auth/password/verify-reset-otp` giới hạn tối đa 5 lần thử sai (`password_reset_otp_fail_count:{userId}`, Redis, cùng TTL với OTP) — vượt ngưỡng → xóa `password_reset_otp:{userId}` ngay (buộc gọi lại `/auth/password/forgot` xin mã mới), không cho thử tiếp trên mã cũ |
| 42 | Revoke session khi reset password | `/auth/password/reset` thành công → tự thu hồi TẤT CẢ session hiện có của user đó (giống hành vi ở mục 34/disable/delete) — đổi mật khẩu qua luồng nghi lộ thì session cũ không nên còn sống |
| 43 | Nơi lưu RSA private key (ký JWT) | User Secrets lúc dev, biến môi trường Coolify lúc go-live — cùng convention đã áp dụng cho `Auth:TotpEncryptionKey` (mục 13) |
| 44 | Anti-bot (CAPTCHA) | **Cloudflare Turnstile** (đã dùng Cloudflare sẵn, miễn phí, không cần thư viện NuGet — verify bằng 1 `HttpClient.PostAsync` tới `challenges.cloudflare.com/turnstile/v0/siteverify`). Bắt buộc ở `/auth/register` và `/auth/password/forgot`; **adaptive** ở `/auth/login`/`/auth/login/password` — chỉ bắt buộc kèm `captchaToken` khi `otp_fail_count >= 1` (tránh làm phiền user hợp lệ mỗi lần login). Không gắn ở `/auth/token` (Flow B, machine-to-machine, không có người giải CAPTCHA) |
| 45 | Đăng nhập/đăng ký bằng Google | Chuẩn **OIDC Authorization Code**, frontend lấy `idToken` từ Google, backend verify chữ ký + `aud` (Client ID) + `iss` + **bắt buộc `email_verified=true`** (thư viện `Google.Apis.Auth` — **hỏi anh trước khi cài**, hoặc official nên rủi ro thấp hơn tự verify JWKS thủ công). Email trùng với user đã có sẵn trong hệ thống → **tự động liên kết** `google_id` vào user đó (chấp nhận được vì Google đã verify quyền sở hữu email); email chưa tồn tại → tạo user mới (mục 46) |
| 46 | Tài khoản Google vẫn bắt buộc TOTP — không có ngoại lệ policy, **Google KHÔNG BAO GIỜ cấp access token trực tiếp** | Google chỉ thay thế bước "xác thực danh tính ban đầu + nhập password", **không thay thế OTP, không bypass check khóa mục 40**. `/auth/login/google` verify idToken → **email chưa tồn tại**: tạo user (`RoleLevel=5`, `google_id`, `email` từ Google, `password_hash = NULL` tạm) → trả `requiresSetup=true` + `setupToken` ngắn hạn (mục 47) → client đặt password (mục 27) qua `/auth/register/google/complete` → sinh `TotpSecret` + trả `otpauthUri` — y hệt luồng `/auth/register` thường từ đây (quét QR, `/auth/login` OTP bình thường). **Email đã tồn tại**: check trạng thái khóa TRƯỚC (y hệt mục 40 — `is_permanently_locked`/`otp_locked_until`/`account_locked_until` đều áp dụng) → liên kết `google_id` nếu chưa có → **KHÔNG cấp token**, chỉ trả `requiresOtp=true` + `username` → client bắt buộc gọi tiếp `/auth/login` (username + OTP) như luồng thường để lấy token. Google ở case này chỉ tương đương bước "xác thực đúng danh tính + đề xuất username", đúng vai trò thay thế username+password, không hơn |
| 47 | Lưu trữ `setupToken` (Google, mục 46) | Redis: `google_setup_token:{tokenHash}` → `userId`, TTL `Auth:GoogleSetupTokenTtl` (mặc định 15 phút), dùng 1 lần (xóa ngay sau khi `/auth/register/google/complete` thành công) — cùng pattern với `password_reset_token` (mục 6) |
| 48 | Quy ước tham số SQL — `v_xxx` + gọi `AddParameter("@xxx", ...)` | Đổi từ `p_xxx` sang **`v_xxx`** cho tên tham số IN trong mọi `sp_*` function (áp dụng ngược cả `ApiCore8` — đã sửa `postgres_users_setup.sql` + `UserRepository.cs`). Call site gọi `_db.AddParameter("@ten_gon", value)` — `PostgresDbHelper.AddParameter` tự bỏ dấu `@` và thêm tiền tố `v_` (VD `"@from_date"` → tham số Postgres `v_from_date`). Không có dấu `@` thì giữ nguyên chuỗi truyền vào (tương thích ngược). `v_out` (REFCURSOR) không đi qua `AddParameter` nên không bị trùng tiền tố |
| 49 | Đọc config — **Options pattern**, không dùng `IConfiguration["X:Y"]` string indexing rải rác | Định nghĩa các class options mạnh kiểu (mục 5): `AuthOptions` (TotpEncryptionKey, RSA private key, OtpLockoutDuration, AccountLockoutDuration, PasswordResetOtpTtl/TokenTtl, GoogleSetupTokenTtl), `SmtpOptions` (mục 38), `TurnstileOptions` (mục 44), `GoogleOptions` (mục 45) — bind bằng `services.Configure<T>(configuration.GetSection("X"))`, inject `IOptions<T>`/`IOptionsSnapshot<T>`. Bật `ValidateDataAnnotations()` + `ValidateOnStart()` (.NET 8) — thiếu key bắt buộc thì **fail ngay lúc start-up**, không phải lỗi runtime lúc gọi API mới phát hiện |

## 2. Cấp bậc (RoleLevel)

Số càng nhỏ, quyền càng cao:

```
1 — Admin
2 — Giám đốc (Director)
3 — Quản lý (Manager)
4 — Nhân viên (Staff)
5 — Khách hàng (Customer)
```

**Kiểm tra quyền:**

- **`CanAccess(actor, requiredMin)`** — actor.Level ≤ requiredMin.Level → cho phép gọi API. Dùng cho ngưỡng cố định (VD "API này cần Quản lý trở lên").
- **Thao tác tác động user khác** (đổi role/disable/xóa/thu hồi session) — **chỉ Admin (cấp 1)** được làm, check cứng `actor.RoleLevel == RoleLevel.Admin`, không phải bậc thang tương đối. Chặn thêm trường hợp actor thao tác lên chính mình (`actor.Id == target.Id` → từ chối, tránh Admin tự khóa/xóa chính tài khoản đang dùng).

## 3. Domain (`AuthService.Domain`)

- **`RoleLevel`** (enum): `Admin=1, Director=2, Manager=3, Staff=4, Customer=5`
- **`User`**: `Id`, `Username`, `Email`, `PasswordHash` (nullable — user mới qua Google chưa hoàn tất setup, mục 46), `GoogleId` (nullable, mục 45), `TotpSecret`, `IsTotpEnabled`, `RoleLevel`, `FailedOtpAttemptCount`, `IsActive`, `IsPermanentlyLocked` (mục 35), + audit columns (mục 4)
- **`Session`**: `Id`, `UserId`, `DeviceInfo`, `IpAddress`, `RefreshTokenHash`, `PreviousRefreshTokenHash` (cho reuse detection, mục 20), `LastActivityAt`, `ExpiresAt` (giới hạn tuyệt đối, mục 21), `IsRevoked`, + audit columns
- **`Client`** (đối tác ngoài, Flow B): `Id`, `ClientId` (unique), `ClientName`, `ClientSecretHash`, `AllowedScopes` (mảng string), `AllowedIps` (mảng CIDR/IP, null = không giới hạn), `IsActive`, + audit columns

## 4. Schema Postgres (`auth_db`) — convention & audit columns

> Script tạo bảng + toàn bộ `sp_*` function đã generate sẵn tại `AuthService/scripts/auth_db_setup.sql` (khớp 100% với mục 4b bên dưới). Script seed Admin đầu tiên tại `AuthService/scripts/auth_db_seed_admin.sql` (mục 8 bước 4). Anh chạy 2 file này trên `auth_db` trước khi bắt đầu code Infrastructure.

Theo đúng pattern hiện có (`scripts/postgres_users_setup.sql`): snake_case, `TIMESTAMPTZ`, `BOOLEAN`, UUID PK `gen_random_uuid()`, data access qua `sp_*` function + `refcursor`, app chỉ có quyền `EXECUTE` (không SELECT/INSERT/UPDATE/DELETE trực tiếp).

Bộ cột audit áp dụng cho **mọi bảng** (map từ chuẩn Oracle anh dùng sang Postgres — `NUMBER(1,0)` → `BOOLEAN` thật, `TIMESTAMP(6)` → `TIMESTAMPTZ` để nhất quán UTC):

```sql
created_date   TIMESTAMPTZ NOT NULL DEFAULT now(),
created_user   VARCHAR(50),
updated_date   TIMESTAMPTZ,
updated_user   VARCHAR(50),
is_deleted     BOOLEAN NOT NULL DEFAULT false,
deleted_date   TIMESTAMPTZ,
deleted_user   VARCHAR(50)
```

- `created_user`: tự đăng ký → chính username đó; do cấp cao hơn thực hiện → username actor lấy từ claim JWT.
- Mọi `sp_*_get_*` phải có `WHERE is_deleted = false`.
- `sessions.user_id` cần cascade xử lý khi user bị xóa (soft delete → tự revoke hết session liên quan trong cùng transaction, không cần FK CASCADE thật vì không hard-delete).

## 4b. Danh sách bảng (`auth_db`) — đầy đủ

**`users`**
| Cột | Kiểu | Ghi chú |
|---|---|---|
| id | UUID PK | `gen_random_uuid()` |
| username | VARCHAR(100) UNIQUE | |
| email | VARCHAR(255) UNIQUE NOT NULL | nhận mã OTP reset password (mục 36, 38) |
| password_hash | VARCHAR(255) NULL | BCrypt, độ dài 8-12 + hoa + ký tự đặc biệt (mục 27). NULL tạm thời cho user mới qua Google chưa hoàn tất setup (mục 46) |
| google_id | VARCHAR(50) UNIQUE NULL | sub claim từ Google ID Token, null nếu chưa liên kết Google (mục 45) |
| totp_secret | TEXT | mã hóa AES (mục 13) |
| is_totp_enabled | BOOLEAN | |
| role_level | SMALLINT | 1-5, xem mục 2 |
| failed_otp_attempt_count | INT | dùng song song Redis counter (mục 17-18 dùng Redis là chính, cột này chỉ để hiển thị/audit). **`password_fail_count`/`account_lockout_count` cố ý KHÔNG có cột riêng** — chỉ sống ở Redis (TTL hết là thôi), lịch sử chi tiết từng lần sai đã có `login_history` (is_success/failure_reason) đảm nhiệm |
| is_active | BOOLEAN | |
| is_permanently_locked | BOOLEAN | khóa vĩnh viễn (mục 35), chỉ gỡ qua luồng reset email (mục 36) hoặc Admin (mục 37) |
| + 7 cột audit | | `created_date/user`, `updated_date/user`, `is_deleted`, `deleted_date/user` (mục 4) |

**`sessions`**
| Cột | Kiểu | Ghi chú |
|---|---|---|
| id | UUID PK | |
| user_id | UUID FK → users.id | |
| device_info | TEXT | |
| ip_address | VARCHAR(45) | hỗ trợ IPv6 |
| refresh_token_hash | VARCHAR(255) | hash hiện tại |
| previous_refresh_token_hash | VARCHAR(255) | cho reuse detection (mục 20) |
| last_activity_at | TIMESTAMPTZ | |
| expires_at | TIMESTAMPTZ | giới hạn tuyệt đối (mục 21) |
| is_revoked | BOOLEAN | |
| + 7 cột audit | | |

**`clients`** (đối tác ngoài, Flow B)
| Cột | Kiểu | Ghi chú |
|---|---|---|
| id | UUID PK | |
| client_id | VARCHAR(100) UNIQUE | |
| client_name | VARCHAR(200) | |
| client_secret_hash | VARCHAR(255) | BCrypt |
| allowed_scopes | TEXT[] | mảng string Postgres |
| allowed_ips | TEXT[] | mảng CIDR/IP, null = không giới hạn |
| is_active | BOOLEAN | |
| + 7 cột audit | | |

**`login_history`** (lịch sử đăng nhập lâu dài — KHÁC `sessions`: ghi mãi mãi kể cả lần thất bại, không dọn dẹp/xóa như session hết hạn)
| Cột | Kiểu | Ghi chú |
|---|---|---|
| id | UUID PK | |
| user_id | UUID FK → users.id (nullable) | null nếu login thất bại với username không tồn tại |
| username_attempted | VARCHAR(100) | ghi cả khi username sai — phát hiện brute-force |
| ip_address | VARCHAR(45) | |
| device_info | TEXT | |
| login_type | VARCHAR(20) | `otp` / `password_fallback` / `refresh` / `password_reset` (mục 36/42) |
| is_success | BOOLEAN | ghi cả lần **thất bại** (sai OTP/password), không chỉ lần thành công |
| failure_reason | VARCHAR(200) | null nếu thành công — VD `wrong_otp`, `account_locked` |
| created_date | TIMESTAMPTZ NOT NULL DEFAULT now() | |

## 5. Application (`AuthService.Application`)

**Interfaces:** `IUserRepository`, `ISessionRepository`, `IOtpService`, `IJwtService`, `IPasswordHasher`, `ITotpSecretEncryption`, `IClientRepository` (bảng `clients`, Flow B), `ILoginHistoryRepository` (bảng `login_history` — ghi mọi lần login thành công/thất bại), **`IRoleAuthorizationService`** (`CanAccess` + check `IsAdmin`/self-target ở mục 2), **`IClientIpResolver`** (lấy IP thật + Trusted Proxy check, mục 24), **`IEmailSender`** (gửi mã OTP reset password, mục 36/38), **`IPasswordResetService`** (sinh/verify OTP reset + reset token, mục 36), **`ICaptchaVerifier`** (verify `captchaToken` với Cloudflare Turnstile, mục 44), **`IGoogleTokenVerifier`** (verify `idToken` Google, mục 45).

**Contracts:** `RegisterRequest/Response`, `LoginRequest`, `LoginWithPasswordRequest`, `TokenResponse`, `SessionDto`, `UserDto`, `ChangeUserRoleRequest`, `ClientTokenRequest` (Flow B, client_credentials), `ForgotPasswordRequest` (`username`), `VerifyResetOtpRequest`/`Response` (`username`, `otp` → `resetToken`), `ResetPasswordRequest` (`resetToken`, `newPassword` → `otpauthUri`).

**`AuthErrorCode` (enum, dùng làm `APIResult.StatusID`)** — mục 40: `InvalidCredential`, `OtpLocked`, `AccountLocked`, `AccountPermanentlyLocked`, ... (mở rộng thêm khi code tới controller).

**Options classes (mục 49)** — nằm ở Application (POCO thuần, không phụ thuộc `Microsoft.Extensions.Options` ở tầng này, chỉ bind ở Api/Infrastructure): `AuthOptions`, `SmtpOptions`, `TurnstileOptions`, `GoogleOptions` — mỗi class có `DataAnnotations` (`[Required]`) trên field bắt buộc để `ValidateOnStart()` bắt lỗi thiếu config ngay lúc chạy.

## 6. Infrastructure (`AuthService.Infrastructure`)

- `UserRepository`/`SessionRepository` — gọi `sp_*` qua `PostgresDbHelper` sẵn có
- `OtpFailCounterService` (Redis) — key thiết kế:
  - `otp_fail_count:{userId}` — tăng khi sai OTP, reset khi đúng; đạt 3 → set `otp_locked_until:{userId} = now + Auth:OtpLockoutDuration` (mặc định 1h) + reset counter. Trong lúc còn khóa, login OTP bị từ chối ngay (dù đúng mã), Password fallback vẫn dùng được — nhưng chỉ khi đã qua gate mục 29.
  - `last_used_otp_step:{userId}` — chống replay cùng chu kỳ 30s (mục 30), TTL ~60s.
  - `password_fail_count:{userId}` — tăng khi sai Password (chỉ tính lúc đang ở fallback); đạt ngưỡng (mặc định 3) → set `account_locked_until:{userId} = now + Auth:AccountLockoutDuration` (mặc định 1h), khóa CẢ OTP lẫn Password, đồng thời tăng `account_lockout_count:{userId}`.
  - `account_lockout_count:{userId}` — đếm số lần bị khóa tạm; từ lần thứ 2 trở đi (sai tiếp sau khi hết khóa tạm) → set `users.is_permanently_locked = true` (Postgres) thay vì khóa tạm lần nữa (mục 35).
  - Admin unlock (`PUT /auth/users/{id}/unlock`) — xóa cả 2 key lock + reset cả 2 counter + `account_lockout_count` + `is_permanently_locked` (mục 37).
- `PasswordResetService` (Redis) — key thiết kế:
  - `password_reset_otp:{userId}` — mã 6 số, TTL `Auth:PasswordResetOtpTtl` (mặc định 10 phút).
  - `password_reset_otp_fail_count:{userId}` — đếm số lần verify sai (mục 41), tối đa 5 lần rồi xóa `password_reset_otp:{userId}` bắt xin mã mới.
  - `password_reset_token:{tokenHash}` → `userId`, sinh sau khi verify OTP đúng, TTL `Auth:PasswordResetTokenTtl` (mặc định 15 phút), dùng 1 lần (xóa ngay sau khi `/auth/password/reset` thành công).
  - `password_reset_otp_cooldown:{userId}` — chặn spam gọi lại `/auth/password/forgot`, TTL 60s (mục 6 dùng trong controller, không phải chỉ thiết kế suông).
- `EmailSender` (MailKit — **hỏi anh trước khi cài**) — implement `IEmailSender`, đọc `SmtpOptions` (Options pattern, mục 49) thay vì `IConfiguration["Smtp:..."]`.
- `TotpService` (Otp.NET), `JwtService` (RSA, nhúng claim `role_level`, `ClockSkew=30s` khi validate ở `ApiCore8.Api` — mục 32), `BCryptPasswordHasher`
- `RoleAuthorizationService` (implement `CanAccess`/`CanManageUser`)
- Custom attribute `[RequireMinRoleLevel(RoleLevel.X)]` — check claim JWT, tái dùng được ở Business API (Bước 5)
- `ClientRepository` (bảng `clients`, Flow B) — verify `client_secret` (BCrypt), check `AllowedIps`, `AllowedScopes`, `IsActive`
- `ClientIpResolver` — đọc `CF-Connecting-IP`/`X-Forwarded-For` **chỉ khi** `RemoteIpAddress` nằm trong dải IP Cloudflare chính thức (`cloudflare.com/ips`, nhúng thành config tĩnh, cần cập nhật thủ công định kỳ vì Cloudflare thỉnh thoảng đổi); ngược lại dùng thẳng `RemoteIpAddress`
- `ClientRateLimiter` (Redis) — rate limit + audit log theo `client_id` cho `/auth/token` (Flow B)
- `CaptchaVerifier` (mục 44) — implement `ICaptchaVerifier`, gọi `HttpClient.PostAsync` tới `challenges.cloudflare.com/turnstile/v0/siteverify` (không cần NuGet mới). Đọc `TurnstileOptions` (Options pattern, mục 49) — `SecretKey` (User Secrets, secret), `SiteKey` (public, appsettings thường — frontend dùng để render widget)
- `GoogleTokenVerifier` (mục 45) — implement `IGoogleTokenVerifier` bằng `Google.Apis.Auth` (`GoogleJsonWebSignature.ValidateAsync`), check `aud` khớp `GoogleOptions.ClientId` (Options pattern, mục 49) + `email_verified=true`
- `GoogleSetupTokenService` (Redis, mục 47) — key `google_setup_token:{tokenHash}` → `userId`, TTL `Auth:GoogleSetupTokenTtl` (mặc định 15 phút), dùng 1 lần

## 7. API — toàn bộ endpoint

| Method | Route | Việc | Quyền yêu cầu |
|---|---|---|---|
| POST | `/auth/register` | Đăng ký — hash password, sinh TOTP secret, bắt buộc kèm `email` + `captchaToken` (mục 44) | Public, luôn ra cấp 5 |
| POST | `/auth/login` | Đăng nhập Username + OTP (luồng chính) — check khóa trước (mục 40), bắt buộc `captchaToken` nếu `otp_fail_count >= 1` (mục 44) | Public |
| POST | `/auth/login/password` | Fallback — chỉ chấp nhận khi đã sai OTP ≥3 lần liên tiếp (mục 29), check khóa trước (mục 40), luôn bắt buộc `captchaToken` (mục 44) | Public |
| POST | `/auth/password/forgot` | Gửi mã OTP reset qua email nếu username tồn tại (response luôn generic — mục 39), bắt buộc `captchaToken` (mục 44), có cooldown chống spam (mục 6) | Public |
| POST | `/auth/password/verify-reset-otp` | Verify mã OTP email → trả `resetToken` dùng 1 lần, tối đa 5 lần thử sai (mục 41) | Public |
| POST | `/auth/password/reset` | Đổi password mới bằng `resetToken` — tự gỡ mọi lock (kể cả vĩnh viễn) + revoke hết session cũ (mục 42) + sinh lại TOTP secret, trả `otpauthUri` mới | Public (cần `resetToken` hợp lệ) |
| POST | `/auth/login/google` | Verify `idToken` Google — email đã có: check khóa trước (mục 40) → liên kết `google_id` → **KHÔNG cấp token**, trả `requiresOtp=true` + `username` để client gọi tiếp `/auth/login`; email chưa có → tạo user mới, trả `requiresSetup=true` + `setupToken` (mục 45, 46, 47) | Public |
| POST | `/auth/register/google/complete` | Hoàn tất setup user mới từ Google: đặt password + sinh TOTP secret, trả `otpauthUri`, bắt buộc `captchaToken` (mục 44, 46) | Cần `setupToken` hợp lệ |
| POST | `/auth/token/refresh` | Refresh access token (rotate refresh token) | Đã đăng nhập |
| POST | `/auth/logout` | Revoke session hiện tại | Đã đăng nhập |
| GET | `/auth/sessions` | Xem session của chính mình | Đã đăng nhập |
| DELETE | `/auth/sessions/{id}` | Tự thu hồi session của mình | Đã đăng nhập |
| DELETE | `/auth/sessions` | Tự thu hồi TẤT CẢ session của chính mình (mục 33) | Đã đăng nhập |
| GET | `/auth/users` | Danh sách user | `RequireMinRoleLevel(Manager)` |
| PUT | `/auth/users/{id}/role` | Đổi role user khác, tự revoke hết session user đó (mục 34) | **Chỉ Admin** + không tự đổi role chính mình |
| PUT | `/auth/users/{id}/disable` | Vô hiệu hóa tài khoản + tự revoke hết session | **Chỉ Admin** + không tự disable chính mình |
| DELETE | `/auth/users/{id}` | Soft-delete tài khoản + tự revoke hết session | **Chỉ Admin** + không tự xóa chính mình |
| GET | `/auth/admin/sessions` | Xem TẤT CẢ session đang active toàn hệ thống | `RequireMinRoleLevel(Manager)` |
| GET | `/auth/users/{id}/sessions` | Xem session của 1 user cụ thể | `RequireMinRoleLevel(Manager)` |
| DELETE | `/auth/users/{id}/sessions` | Thu hồi toàn bộ session của user đó | **Chỉ Admin** |
| DELETE | `/auth/users/{id}/sessions/{sessionId}` | Thu hồi 1 session cụ thể của user đó | **Chỉ Admin** |
| PUT | `/auth/users/{id}/unlock` | Mở khóa OTP-lock/account-lock **+ khóa vĩnh viễn**, reset fail counter (mục 37, phương án dự phòng cuối) | **Chỉ Admin** |
| POST | `/auth/token` | **Flow B** — OAuth2 Client Credentials Grant cho đối tác ngoài (`Authorization: Basic base64(client_id:client_secret)`) | Public (verify bằng client_secret) |
| PUT | `/clients/{id}/regenerate-secret` | Sinh `client_secret` mới, secret cũ vô hiệu ngay | **Chỉ Admin** |
| PUT | `/clients/{id}/disable` | Khóa khẩn cấp 1 đối tác | **Chỉ Admin** |

## 8. Thứ tự code (checkpoint từng bước, không code 1 lèo — mỗi ý xong tick [x] vào đây, xong hết 1 Bước mới sang Bước kế)

### Bước 1 — Domain + Application (chưa chạm DB, chưa cài thư viện)

- [ ] `AuthService.Domain`: enum `RoleLevel`
- [ ] `AuthService.Domain`: base audit fields (7 cột, mục 4) — dùng chung cho `User`/`Session`/`Client`
- [ ] `AuthService.Domain`: entity `User` (mục 3)
- [ ] `AuthService.Domain`: entity `Session` (mục 3)
- [ ] `AuthService.Domain`: entity `Client` (mục 3)
- [ ] `AuthService.Application`: enum `AuthErrorCode` (mục 40)
- [ ] `AuthService.Application`: Options classes `AuthOptions`/`SmtpOptions`/`TurnstileOptions`/`GoogleOptions` (mục 49)
- [ ] `AuthService.Application`: Contracts — Register/Login/Token/Session/User/Client DTO (mục 5)
- [ ] `AuthService.Application`: Contracts — Forgot/Verify/Reset password DTO + Google DTO (mục 5)
- [ ] `AuthService.Application`: interface `IUserRepository`, `ISessionRepository`, `IClientRepository`, `ILoginHistoryRepository`
- [ ] `AuthService.Application`: interface `IOtpService`, `IJwtService`, `IPasswordHasher`, `ITotpSecretEncryption`
- [ ] `AuthService.Application`: interface `IRoleAuthorizationService`, `IClientIpResolver`
- [ ] `AuthService.Application`: interface `IEmailSender`, `IPasswordResetService`, `ICaptchaVerifier`, `IGoogleTokenVerifier`
- [ ] `dotnet build AuthService.sln` — 0 lỗi

### Bước 2 — Infrastructure

- [ ] **Anh chạy `AuthService/scripts/auth_db_setup.sql` trên `auth_db` trước** (chặn code tiếp tới khi xong)
- [ ] **Anh duyệt cài `MailKit` + `Google.Apis.Auth`** (chặn code tiếp tới khi xong)
- [ ] `UserRepository` (`sp_user_*` qua `PostgresDbHelper`)
- [ ] `SessionRepository` (`sp_session_*`)
- [ ] `ClientRepository` (`sp_client_*`)
- [ ] `LoginHistoryRepository` (`sp_login_history_*`)
- [ ] `OtpFailCounterService` (Redis — mục 6)
- [ ] `PasswordResetService` (Redis — mục 6)
- [ ] `GoogleSetupTokenService` (Redis — mục 47)
- [ ] `TotpService` (Otp.NET)
- [ ] `TotpSecretEncryption` (AES, mục 13)
- [ ] `JwtService` (RSA, ký + issue, mục 4/15/16/43)
- [ ] `BCryptPasswordHasher`
- [ ] `RoleAuthorizationService` (`CanAccess`, self-target check, mục 2)
- [ ] `ClientIpResolver` (Cloudflare IP check, mục 24)
- [ ] `EmailSender` (MailKit, mục 38)
- [ ] `CaptchaVerifier` (Turnstile, mục 44)
- [ ] `GoogleTokenVerifier` (Google.Apis.Auth, mục 45)
- [ ] `ClientRateLimiter` (Redis, Flow B mục 25)
- [ ] `AddInfrastructureServices()` — wiring DI đầy đủ
- [ ] `dotnet build AuthService.sln` — 0 lỗi

### Bước 3 — Api

- [ ] `StartupConfig`: JWT Bearer auth (RS256, ClockSkew 30s); bind `AuthOptions`/`SmtpOptions`/`TurnstileOptions`/`GoogleOptions` qua `Configure<T>()` + `ValidateDataAnnotations()` + `ValidateOnStart()` (mục 49); config Redis/Postgres
- [ ] Attribute `[RequireMinRoleLevel(RoleLevel.X)]`
- [ ] `AuthController`: `/auth/register`
- [ ] `AuthController`: `/auth/login`, `/auth/login/password`
- [ ] `AuthController`: `/auth/password/forgot`, `/verify-reset-otp`, `/reset`
- [ ] `AuthController`: `/auth/login/google`, `/auth/register/google/complete`
- [ ] `AuthController`: `/auth/token/refresh`, `/auth/logout`
- [ ] `SessionController`: `GET/DELETE /auth/sessions`, `DELETE /auth/sessions/{id}`
- [ ] `UserController`: `GET /auth/users`, role/disable/delete/unlock, admin session endpoints
- [ ] `TokenController`: `POST /auth/token` (Flow B)
- [ ] `ClientController`: regenerate-secret/disable (Admin)
- [ ] Swagger annotations + kiểm tra load được
- [ ] `dotnet build AuthService.sln` — 0 lỗi, chạy thử `AuthService.Api` lên được

### Bước 4 — SQL seed Admin

- [ ] Anh tự sinh BCrypt hash + TOTP secret mã hóa AES (theo hướng dẫn trong file)
- [ ] Chạy `AuthService/scripts/auth_db_seed_admin.sql`
- [ ] Verify `SELECT` thấy user `admin` role_level=1

### Bước 5 — Unit test

- [ ] Test OTP verify + gate `/auth/login/password` (mục 29)
- [ ] Test replay OTP cùng chu kỳ 30s (mục 30)
- [ ] Test 3-strike OTP → fallback → 3-strike password → khóa tạm → khóa vĩnh viễn (mục 35)
- [ ] Test luồng reset password/email OTP end-to-end (mục 36, mock `IEmailSender`)
- [ ] Test JWT issue/validate + `ClockSkew` (mục 32)
- [ ] Test `CanAccess`/self-target check (mục 2)
- [ ] Test refresh token reuse detection (mục 20)
- [ ] Test `ClientIpResolver` (Trusted Proxy check, mục 24)
- [ ] Test Google flow (mock `IGoogleTokenVerifier`) — tạo user mới vs liên kết user có sẵn, không bypass OTP (mục 46)
- [ ] `dotnet test` — toàn bộ pass

### Bước 6 — Verify tay qua Swagger

- [ ] Đăng ký → quét QR → đăng nhập OTP → refresh token
- [ ] Test RBAC giữa các cấp (`CanAccess`)
- [ ] Test bộ endpoint quản lý session/user (Admin)
- [ ] Test luồng quên mật khẩu/khóa vĩnh viễn (email thật)
- [ ] Test Flow B (`/auth/token` với client_secret)
- [ ] Test Google Sign-In (lấy `idToken` qua Google OAuth Playground, dán vào Swagger)

## 9. Ví dụ luồng: đăng ký → gọi API có bảo vệ (Business API)

```
1) POST AuthService /auth/register
   Body: { "username": "hieu", "password": "Abc@12345" }
   → AuthService: hash password (BCrypt), sinh TotpSecret (mã hóa AES trước khi lưu DB),
     tạo user RoleLevel=5 (Khách hàng)
   ← Response: { "otpauthUri": "otpauth://totp/AuthService:hieu?secret=JBSWY3DP...&issuer=AuthService" }

2) Anh copy chuỗi "JBSWY3DP..." nhập tay vào Google Authenticator (Add account → Enter setup key)
   → App bắt đầu sinh mã 6 số, đổi mỗi 30s

3) POST AuthService /auth/login
   Body: { "username": "hieu", "otp": "123456" }
   → AuthService: verify OTP (Otp.NET, so với TotpSecret đã giải mã),
     tạo Session mới (device/IP/session_id), issue JWT (RSA, exp 5 phút, claim: sub=userId,
     role_level=5, session_id=<session_id>)
   ← Response: { "accessToken": "eyJhbGci...", "refreshToken": "a1b2c3..." }

4) GET ApiCore8.Api /api/products   (giả định 1 API business bất kỳ, gắn [Authorize])
   Header: Authorization: Bearer eyJhbGci...
   → ApiCore8.Api (JWT Bearer middleware, dùng RSA PUBLIC key của AuthService để verify):
     a. Verify chữ ký JWT hợp lệ + chưa hết hạn (exp)
     b. Lấy claim session_id → tra Redis key revoked_session:{session_id}
        - Nếu KHÔNG có trong Redis (chưa bị revoke) → cho qua, xử lý API bình thường
        - Nếu CÓ trong Redis (đã bị Admin revoke/logout) → trả 401, dù chữ ký JWT vẫn hợp lệ
     c. (Nếu API yêu cầu RoleLevel cụ thể, VD [RequireMinRoleLevel(Staff)]) so claim
        role_level với ngưỡng — 5 (Khách hàng) không qua được ngưỡng Staff(4) → 403
   ← Response: 200 OK (dữ liệu sản phẩm) hoặc 401/403 tùy kết quả check

5) Sau 5 phút, accessToken hết hạn tự nhiên
   → Client gọi POST /auth/token/refresh với refreshToken
     → AuthService verify refreshToken hash khớp Session, session chưa bị revoke/hết hạn
       → issue accessToken mới (rotate refreshToken luôn, refreshToken cũ vô hiệu)

6) Nếu Admin vào /auth/admin/sessions thấy session của "hieu", bấm revoke:
   DELETE /auth/users/{hieu_id}/sessions/{session_id}   (chỉ Admin gọi được)
   → AuthService: set session.is_revoked=true + ghi key revoked_session:{session_id} vào Redis
   → Lần gọi API tiếp theo của "hieu" (dù accessToken cũ chưa hết 5 phút) → bị chặn ở bước 4b,
     phải đăng nhập lại (OTP) để có session/token mới
```

## 9b. Ví dụ luồng B: đối tác ngoài (Client Credentials Grant)

```
1) [1 lần, ngoài API] Admin tạo record `clients`:
   client_id="facebook-partner", client_secret=<random, chỉ show 1 lần>,
   allowed_scopes=["products:read"], allowed_ips=["203.0.113.0/24"]
   → gửi client_id/secret cho đối tác qua kênh riêng (không qua API)

2) Đối tác gọi:
   POST /auth/token
   Header: Authorization: Basic base64("facebook-partner:<secret>")
   Body: grant_type=client_credentials

3) AuthService xử lý:
   a. Lấy IP thật qua ClientIpResolver (CF-Connecting-IP, chỉ tin nếu RemoteIpAddress
      đúng là Cloudflare) → so với allowed_ips của "facebook-partner" → không khớp thì 403
   b. So client_secret với hash đã lưu (BCrypt) → khớp
   c. Ghi audit log (client_id, IP, thời gian) + tăng rate-limit counter
   ← { "accessToken": "eyJ...", "expiresIn": 300 }   (JWT RSA, claim client_id, scopes)

4) Đối tác gọi GET ApiCore8.Api /api/products
   Header: Authorization: Bearer eyJ...
   → verify RS256 + check claim "scopes" chứa "products:read" → 200
   → Gọi /api/orders (ngoài scope) → 403 dù token hợp lệ

5) Nếu nghi lộ secret:
   PUT /clients/{id}/regenerate-secret   (Admin) → secret cũ vô hiệu ngay
   PUT /clients/{id}/disable             (Admin) → khóa khẩn cấp toàn bộ
```

## 9c. Ví dụ luồng: Quên mật khẩu / Tài khoản bị khóa vĩnh viễn (mục 36)

```
1) Client (nhãn "Quên mật khẩu" hoặc "Cấp lại quyền truy cập" — cùng gọi chung API):
   POST /auth/password/forgot
   Body: { "username": "hieu" }
   → AuthService: tìm user theo username (không lộ tồn tại hay không, mục 39),
     nếu có → sinh mã 6 số, lưu password_reset_otp:{userId} (TTL 10 phút), gửi email (MailKit)
   ← Response: 200, message chung chung ("Nếu tài khoản tồn tại, mã xác nhận đã được gửi")

2) User mở email, lấy mã 6 số, nhập vào client:
   POST /auth/password/verify-reset-otp
   Body: { "username": "hieu", "otp": "482910" }
   → AuthService: so khớp password_reset_otp:{userId}, đúng → sinh resetToken (random, hash lưu
     password_reset_token:{tokenHash} → userId, TTL 15 phút), xóa OTP đã dùng
   ← Response: { "resetToken": "r3s3t..." }
   → Client điều hướng sang màn hình nhập password mới, giữ resetToken (không hiển thị cho user)

3) User nhập password mới:
   POST /auth/password/reset
   Body: { "resetToken": "r3s3t...", "newPassword": "Abc@2025!" }
   → AuthService: verify resetToken hợp lệ + chưa dùng → validate password policy (mục 27)
     → update password_hash (BCrypt) → xóa otp_fail_count/password_fail_count/
       account_locked_until/account_lockout_count/is_permanently_locked →
       sinh TotpSecret mới (mã hóa AES) → xóa resetToken
   ← Response: { "otpauthUri": "otpauth://totp/AuthService:hieu?secret=NEWSECRET...&issuer=AuthService" }

4) User quét lại QR vào Google Authenticator → đăng nhập bình thường qua bước 3 ở mục 9
   (lần login OTP đầu tiên bằng secret mới chính là bước xác nhận enrollment thành công)

5) Nếu email đã đăng ký cũng không còn truy cập được → user liên hệ Admin ngoài hệ thống,
   Admin gọi PUT /auth/users/{id}/unlock để gỡ is_permanently_locked (mục 37, dự phòng cuối)
```

## 10. Thư viện cần cài (hỏi trước khi `dotnet add package`)

- `BCrypt.Net-Next`
- `Otp.NET`
- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `System.IdentityModel.Tokens.Jwt`
- `MailKit` — gửi email mã OTP reset password (mục 36, 38)
- `Google.Apis.Auth` — verify ID Token Google Sign-In (mục 45)
