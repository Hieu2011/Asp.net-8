'use server';

import { cookies } from 'next/headers';

import { callAuthApi, toActionResult } from '@/lib/auth/auth-api';
import { QR_LOGIN_COOKIE, buildAuthCookies, buildQrLoginCookie } from '@/lib/auth/auth-cookies';
import { getAccessToken } from '@/lib/auth/session';

import { AUTH_ERROR_CODE } from './types';
import type { ActionResult, QrLoginStatus, QrPollResponse, QrStartResponse } from './types';

type QrStartResult = { challengeId: string; expiresIn: number; pollInterval: number };
type SuccessResponse = { success: boolean };

const CHALLENGE_PATTERN = /^[A-Za-z0-9_-]{10,64}$/;

const invalidChallenge: ActionResult<never> = {
  ok: false,
  code: AUTH_ERROR_CODE.QrChallengeInvalid,
  message: 'Mã QR không hợp lệ',
};

/** Máy tính xin mã QR — pollSecret cất vào cookie httpOnly, trình duyệt chỉ nhận challengeId để vẽ QR */
export async function startQrLoginAction(): Promise<ActionResult<QrStartResult>> {
  const objResult = toActionResult(await callAuthApi<QrStartResponse>('/auth/qr/start'));
  if (!objResult.ok) return objResult;
  const { challengeId, pollSecret, expiresIn, pollInterval } = objResult.data;
  const objCookie = buildQrLoginCookie(`${challengeId}.${pollSecret}`, expiresIn);
  (await cookies()).set(objCookie.name, objCookie.value, objCookie.options);
  return { ok: true, data: { challengeId, expiresIn, pollInterval } };
}

/** Hỏi trạng thái; được duyệt thì ghi cookie đăng nhập ngay tại server rồi xóa cookie QR */
export async function pollQrLoginAction(): Promise<ActionResult<{ status: QrLoginStatus }>> {
  const objCookieStore = await cookies();
  const [strChallengeID, strPollSecret] = (objCookieStore.get(QR_LOGIN_COOKIE)?.value ?? '').split('.');
  if (!strChallengeID || !strPollSecret) return { ok: true, data: { status: 'expired' } };

  const objResult = toActionResult(
    await callAuthApi<QrPollResponse>('/auth/qr/poll', {
      body: { challengeId: strChallengeID, pollSecret: strPollSecret },
    }),
  );
  if (!objResult.ok) return objResult;
  const { status, token } = objResult.data;
  if (status === 'approved' && token) {
    for (const objCookie of buildAuthCookies(token))
      objCookieStore.set(objCookie.name, objCookie.value, objCookie.options);
  }
  if (status === 'approved' || status === 'rejected' || status === 'expired') objCookieStore.delete(QR_LOGIN_COOKIE);
  return { ok: true, data: { status } };
}

/** Điện thoại (đã đăng nhập) đồng ý / từ chối cho máy tính đăng nhập */
export async function decideQrLoginAction(
  strChallengeID: string,
  bolApprove: boolean,
): Promise<ActionResult<SuccessResponse>> {
  if (!CHALLENGE_PATTERN.test(strChallengeID)) return invalidChallenge;
  const strAccessToken = await getAccessToken();
  if (!strAccessToken) return { ok: false, code: 401, message: 'Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại' };
  return toActionResult(
    await callAuthApi<SuccessResponse>(`/auth/qr/${strChallengeID}/${bolApprove ? 'approve' : 'reject'}`, {
      accessToken: strAccessToken,
    }),
  );
}
