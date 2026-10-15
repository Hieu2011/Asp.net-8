import 'server-only';

import { callAuthApi, toActionResult } from '@/lib/auth/auth-api';
import { getAccessToken } from '@/lib/auth/session';

import type { ActionResult, QrChallengeInfo } from './types';

/** Điện thoại mở link QR: đánh dấu đã quét + lấy thông tin máy đang xin đăng nhập */
export async function scanQrChallenge(strChallengeID: string): Promise<ActionResult<QrChallengeInfo>> {
  const strAccessToken = await getAccessToken();
  if (!strAccessToken) return { ok: false, code: 401, message: 'Phiên đăng nhập đã hết hạn' };
  return toActionResult(
    await callAuthApi<QrChallengeInfo>(`/auth/qr/${encodeURIComponent(strChallengeID)}/scan`, {
      accessToken: strAccessToken,
    }),
  );
}
