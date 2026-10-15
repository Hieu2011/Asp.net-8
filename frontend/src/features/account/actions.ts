'use server';

import { revalidatePath } from 'next/cache';
import { cookies } from 'next/headers';

import { z } from 'zod';

import { AUTH_ERROR_CODE } from '@/features/auth/types';
import type { ActionResult } from '@/features/auth/types';
import { callAuthApi, toActionResult } from '@/lib/auth/auth-api';
import { buildClearedAuthCookies } from '@/lib/auth/auth-cookies';
import { getAccessToken } from '@/lib/auth/session';

type SuccessResponse = { success: boolean };

const notSignedIn: ActionResult<never> = {
  ok: false,
  code: 401,
  message: 'Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại',
};

// Server Action công khai → tự kiểm tra đăng nhập, quyền thật do AuthService kiểm (chỉ thu hồi phiên của chính mình)
export async function revokeSessionAction(strSessionID: string): Promise<ActionResult<SuccessResponse>> {
  if (!z.uuid().safeParse(strSessionID).success) {
    return { ok: false, code: AUTH_ERROR_CODE.ValidationError, message: 'Phiên không hợp lệ' };
  }
  const strAccessToken = await getAccessToken();
  if (!strAccessToken) return notSignedIn;
  const objResult = toActionResult(
    await callAuthApi<SuccessResponse>(`/auth/sessions/${strSessionID}`, {
      method: 'DELETE',
      accessToken: strAccessToken,
    }),
  );
  revalidatePath('/account');
  return objResult;
}

/** Đăng xuất khỏi mọi thiết bị, kể cả thiết bị đang dùng */
export async function revokeAllSessionsAction(): Promise<ActionResult<SuccessResponse>> {
  const strAccessToken = await getAccessToken();
  if (!strAccessToken) return notSignedIn;
  const objResult = toActionResult(
    await callAuthApi<SuccessResponse>('/auth/sessions', { method: 'DELETE', accessToken: strAccessToken }),
  );
  if (!objResult.ok) return objResult;
  // Thu hồi tất cả gồm cả phiên hiện tại → xóa cookie luôn để trình duyệt này cũng đăng xuất
  const objCookieStore = await cookies();
  for (const objCookie of buildClearedAuthCookies())
    objCookieStore.set(objCookie.name, objCookie.value, objCookie.options);
  return objResult;
}
