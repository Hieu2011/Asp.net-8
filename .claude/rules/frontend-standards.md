# Frontend standards (`frontend/` — Next.js 16)

Một kiểu code duy nhất, đồng bộ tinh thần `coding-standards.md` của backend. Phần in **(ESLint)** được `eslint.config.mjs` tự chặn — sai là không commit được (husky + lint-staged) và CI fail.

## 0. Trước khi code
- Next 16 khác nhiều so với kiến thức cũ → đọc guide trong `frontend/node_modules/next/dist/docs/` trước khi dùng API lạ (proxy, cache, async params...).
- Chạy `pnpm check` (typecheck + lint + prettier) trước khi báo xong; thay đổi lớn chạy thêm `pnpm build`.
- Cài package mới phải hỏi anh trước.

## 1. Tiền tố biến (ESLint)
Áp cho **biến local + tham số hàm**, giống backend:

| Kiểu | Tiền tố | Ví dụ |
|---|---|---|
| `string` | `str` | `strUserName` |
| `number` | `int` / `dec` / `num` | `intPageIndex`, `decTotalAmount` |
| `boolean` | `bol` | `bolIsLoading` |
| mảng | `lst` / `arr` | `lstProduct` |
| object/DTO/instance | `obj` | `objUser` (quy ước, review kiểm tra) |
| `Date` | `dtm` | `dtmCreatedAt` (quy ước) |

**Miễn tiền tố:** component/type/interface (PascalCase), hook `useXxx`, store `useXxxStore`, handler `handleXxx`, hằng số module `UPPER_SNAKE_CASE`, field của props/DTO/object (hợp đồng JSON, camelCase), biến destructure (`const { data } = ...`, `const [bolOpen, setBolOpen] = useState(false)` — vẫn nên đặt tiền tố cho state), biến 1 ký tự (`x`, `i`), tên bắt đầu `_`, export đặc biệt của Next (`dynamic`, `revalidate`...). Viết tắt `ID` luôn hoa: `strProductID`.

## 2. Đặt tên file & cấu trúc
- File/thư mục **kebab-case**: `login-form.tsx`, `use-cart-store.ts`. 1 component/file, **named export** (trừ `page/layout/loading/error/route` của Next dùng default theo quy ước Next).
- Feature-based:
  ```
  src/app/(auth)/...  (shop)/...  api/...   # route — mỏng, chỉ ghép component
  src/components/ui/        # shadcn sinh ra — không sửa logic, chỉ chỉnh style
  src/components/layout/    # header, nav, footer
  src/features/<tên>/       # components/, hooks/, api.ts, schema.ts, store.ts, types.ts
  src/lib/                  # http client, env, utils dùng chung
  src/proxy.ts              # Next 16: thay middleware.ts
  ```
- Code dùng chung ≥ 2 feature → chuyển lên `src/lib` hoặc `src/components`.

## 3. Component
- **Server Component mặc định**; chỉ thêm `'use client'` khi cần state/effect/event/browser API — đặt ở component lá, không ở layout/page.
- Props khai `type XxxProps = {...}` ngay trên component; không `React.FC`.
- Thứ tự trong component: hook → state → biến dẫn xuất → handler → effect → return.
- Class Tailwind ghép bằng `cn()`; không inline style trừ giá trị động.
- Ảnh dùng `next/image`, link dùng `next/link`.

## 4. State & dữ liệu
- Dữ liệu từ API: **TanStack Query** (`useQuery`/`useMutation`), query key khai trong `features/<x>/api.ts`.
- State client (giỏ hàng, UI): **Zustand** — 1 store/feature, selector khi đọc (`useCartStore((x) => x.lstItem)`).
- **Không bao giờ** lưu token/thông tin nhạy cảm trong Zustand/localStorage — auth đi qua BFF + cookie httpOnly.
- Form: `react-hook-form` + `zod` (schema đặt ở `schema.ts`, dùng lại được phía server).
- Env: đọc qua `@/lib/env.server` (server-only) / `@/lib/env.client` (NEXT_PUBLIC_*), không gọi `process.env` rải rác.

## 5. TypeScript (ESLint)
- `strict` + `noUncheckedIndexedAccess`; cấm `any` (dùng `unknown` + thu hẹp kiểu).
- `import type` cho import chỉ dùng kiểu.
- So sánh `===`; `const` mặc định.

## 6. Format (Prettier — tự sửa khi commit)
- 2 space, single quote, có `;`, printWidth 120, trailing comma, LF.
- Không khoảng trắng cuối dòng, tối đa 1 dòng trống, cuối file 1 dòng trống.
- Class Tailwind tự sắp xếp (`prettier-plugin-tailwindcss`).
- Import theo nhóm, cách 1 dòng **(ESLint)**: React/Next → thư viện → `@/` → tương đối; trong nhóm theo ABC.

## 7. Comment & dọn dẹp
- Comment tiếng Việt, ngắn, nói *tại sao*; JSDoc 1 dòng cho hàm export khó hiểu.
- Không `console.log` **(ESLint)** (được `console.warn/error`), không code chết, không code comment-out.

## 8. Gọi API & bảo mật
- Browser chỉ gọi `/api/*` của chính Next (BFF); URL backend chỉ có ở server env.
- Lỗi từ backend dạng `APIResult` (`isError`, `statusID`, `message`) → map sang thông báo tiếng Việt, không hiện stack trace.
- Không render HTML từ input người dùng (`dangerouslySetInnerHTML`) khi chưa sanitize.
