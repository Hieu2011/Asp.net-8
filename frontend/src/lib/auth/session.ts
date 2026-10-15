import 'server-only';
import { cookies } from 'next/headers';
import { cache } from 'react';

import type { UserProfile } from '@/features/auth/types';

import { callAuthApi } from './auth-api';
import { ACCESS_TOKEN_COOKIE } from './auth-cookies';

export async function getAccessToken(): Promise<string | undefined> {
  return (await cookies()).get(ACCESS_TOKEN_COOKIE)?.value;
}

/** User đang đăng nhập (null nếu chưa) — cache theo request, header/page gọi nhiều lần chỉ tốn 1 lần gọi API */
export const getCurrentUser = cache(async (): Promise<UserProfile | null> => {
  const strAccessToken = await getAccessToken();
  if (!strAccessToken) return null;
  const objResult = await callAuthApi<UserProfile>('/auth/me', { method: 'GET', accessToken: strAccessToken });
  return objResult.isError ? null : objResult.resultObject;
});
