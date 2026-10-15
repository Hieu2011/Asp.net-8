import type { TokenResponse } from '@/features/auth/types';

export const ACCESS_TOKEN_COOKIE = 'hpm_at';
export const REFRESH_TOKEN_COOKIE = 'hpm_rt';
// Giữ challengeId + pollSecret của đăng nhập QR — pollSecret không bao giờ xuống JS trình duyệt
export const QR_LOGIN_COOKIE = 'hpm_qr';
// Khớp SessionAbsoluteTtl (12h) của AuthService
const SESSION_MAX_AGE_SECONDS = 12 * 60 * 60;

type AuthCookie = {
  name: string;
  value: string;
  options: { httpOnly: true; secure: boolean; sameSite: 'lax'; path: '/'; maxAge: number };
};

const buildCookie = (strName: string, strValue: string, intMaxAge: number): AuthCookie => ({
  name: strName,
  value: strValue,
  options: {
    httpOnly: true,
    secure: process.env.NODE_ENV === 'production',
    sameSite: 'lax',
    path: '/',
    maxAge: intMaxAge,
  },
});

/** Cookie httpOnly chứa token — access hết hạn đúng lúc token hết hạn → proxy biết để refresh */
export const buildAuthCookies = (objToken: TokenResponse): AuthCookie[] => [
  buildCookie(ACCESS_TOKEN_COOKIE, objToken.accessToken, objToken.expiresIn),
  buildCookie(REFRESH_TOKEN_COOKIE, objToken.refreshToken, SESSION_MAX_AGE_SECONDS),
];

export const buildQrLoginCookie = (strValue: string, intMaxAge: number) =>
  buildCookie(QR_LOGIN_COOKIE, strValue, intMaxAge);

export const buildClearedAuthCookies = (): AuthCookie[] => [
  buildCookie(ACCESS_TOKEN_COOKIE, '', 0),
  buildCookie(REFRESH_TOKEN_COOKIE, '', 0),
];
