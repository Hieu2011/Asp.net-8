import 'server-only';

import type { SessionInfo } from '@/features/auth/types';
import { callAuthApi } from '@/lib/auth/auth-api';
import { getAccessToken } from '@/lib/auth/session';

/** Các phiên đăng nhập của user hiện tại (null nếu lỗi/hết phiên) */
export async function getMySessions(): Promise<SessionInfo[] | null> {
  const strAccessToken = await getAccessToken();
  if (!strAccessToken) return null;
  const objResult = await callAuthApi<SessionInfo[]>('/auth/sessions', { method: 'GET', accessToken: strAccessToken });
  return objResult.isError ? null : objResult.resultObject;
}
