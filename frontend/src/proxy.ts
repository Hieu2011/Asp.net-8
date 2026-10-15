import { NextResponse } from 'next/server';
import type { NextRequest } from 'next/server';

import { AUTH_ERROR_CODE } from '@/features/auth/types';
import type { ApiResult, TokenResponse } from '@/features/auth/types';
import { callAuthApi } from '@/lib/auth/auth-api';
import {
  ACCESS_TOKEN_COOKIE,
  REFRESH_TOKEN_COOKIE,
  buildAuthCookies,
  buildClearedAuthCookies,
} from '@/lib/auth/auth-cookies';

const PROTECTED_PREFIXES = ['/account', '/checkout', '/orders', '/admin', '/qr-login'];
const AUTH_PAGES = ['/login', '/register', '/forgot-password'];
const REFRESH_SHARE_MS = 10_000;

// Nhiều request song song (nhiều tab, RSC) cùng cầm 1 refresh token → chỉ gọi backend 1 lần, các request khác dùng chung kết quả.
// Giữ kết quả thêm 10s cho request tới trễ vẫn mang cookie cũ (backend đã xoay token, gọi lại sẽ bị coi là dùng lại).
const dicRefreshInFlight = new Map<string, Promise<ApiResult<TokenResponse>>>();

// Chỉ check nhanh bằng cookie (optimistic) — quyền thật vẫn do AuthService kiểm tra khi gọi API
export async function proxy(request: NextRequest) {
  const strPath = request.nextUrl.pathname;
  const strRefreshToken = request.cookies.get(REFRESH_TOKEN_COOKIE)?.value;
  let bolLoggedIn = request.cookies.has(ACCESS_TOKEN_COOKIE);
  let objResponse = NextResponse.next();

  // Access token hết hạn (cookie tự mất) nhưng còn refresh token → làm mới ngay tại đây.
  // Prefetch cũng đi qua đây (Next 16 xóa header next-router-prefetch trước proxy) — refreshOnce gộp nên không bị coi là dùng lại token
  if (!bolLoggedIn && strRefreshToken) {
    const objResult = await refreshOnce(strRefreshToken, request.headers);
    // Request song song khác vừa xoay token (backend trả 37) → giữ nguyên cookie, trình duyệt nhận cookie mới từ request kia
    if (objResult.statusID === AUTH_ERROR_CODE.RefreshTokenConcurrent) return objResponse;
    const lstCookie =
      !objResult.isError && objResult.resultObject
        ? buildAuthCookies(objResult.resultObject)
        : buildClearedAuthCookies();
    bolLoggedIn = !objResult.isError;
    // Ghi cookie mới vào request để Server Component trong cùng request đọc được, và vào response cho browser
    for (const objCookie of lstCookie) request.cookies.set(objCookie.name, objCookie.value);
    objResponse = NextResponse.next({ request: { headers: request.headers } });
    for (const objCookie of lstCookie) objResponse.cookies.set(objCookie.name, objCookie.value, objCookie.options);
  }

  if (!bolLoggedIn && PROTECTED_PREFIXES.some((x) => strPath.startsWith(x))) {
    const objLoginUrl = new URL('/login', request.nextUrl);
    objLoginUrl.searchParams.set('next', strPath);
    return redirectWith(objLoginUrl, objResponse);
  }
  if (bolLoggedIn && AUTH_PAGES.includes(strPath)) return redirectWith(new URL('/', request.nextUrl), objResponse);
  return objResponse;
}

function refreshOnce(strRefreshToken: string, objHeaders: Headers): Promise<ApiResult<TokenResponse>> {
  const tskExisting = dicRefreshInFlight.get(strRefreshToken);
  if (tskExisting) return tskExisting;
  const tskRefresh = callAuthApi<TokenResponse>('/auth/token/refresh', {
    body: { refreshToken: strRefreshToken },
    forwardHeaders: objHeaders,
  });
  dicRefreshInFlight.set(strRefreshToken, tskRefresh);
  setTimeout(() => dicRefreshInFlight.delete(strRefreshToken), REFRESH_SHARE_MS);
  return tskRefresh;
}

// Redirect nhưng giữ cookie vừa refresh/xóa
function redirectWith(objUrl: URL, objSource: NextResponse): NextResponse {
  const objRedirect = NextResponse.redirect(objUrl);
  for (const objCookie of objSource.cookies.getAll()) objRedirect.cookies.set(objCookie);
  return objRedirect;
}

export const config = {
  matcher: ['/((?!api|_next/static|_next/image|favicon.ico|.*\\.(?:png|jpg|jpeg|svg|webp|ico)$).*)'],
};
