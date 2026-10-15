import 'server-only';
import { headers } from 'next/headers';

import { AUTH_ERROR_CODE } from '@/features/auth/types';
import type { ActionResult, ApiResult } from '@/features/auth/types';
import { getServerEnv } from '@/lib/env.server';

type CallOptions = {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE';
  body?: unknown;
  accessToken?: string;
  /** Header request gốc của người dùng (proxy truyền vào; mặc định lấy từ request hiện tại) */
  forwardHeaders?: Headers;
};

const failResult = <T>(intCode: number, strMessage: string): ApiResult<T> => ({
  isError: true,
  statusID: intCode,
  message: strMessage,
  messageDetail: '',
  resultObject: null,
});

/** Gọi AuthService từ server Next — chuyển tiếp IP + User-Agent để backend ghi session/rate limit đúng người */
export async function callAuthApi<T>(strPath: string, objOption: CallOptions = {}): Promise<ApiResult<T>> {
  const { method = 'POST', body, accessToken, forwardHeaders } = objOption;
  const objIncoming = forwardHeaders ?? (await headers());
  const dicHeader: Record<string, string> = { Accept: 'application/json' };
  if (body !== undefined) dicHeader['Content-Type'] = 'application/json';
  if (accessToken) dicHeader.Authorization = `Bearer ${accessToken}`;
  const strClientIP = objIncoming.get('cf-connecting-ip') ?? objIncoming.get('x-forwarded-for')?.split(',')[0]?.trim();
  if (strClientIP) dicHeader['X-Forwarded-For'] = strClientIP;
  const strUserAgent = objIncoming.get('user-agent');
  if (strUserAgent) dicHeader['User-Agent'] = strUserAgent;
  const objUrl = new URL(strPath, getServerEnv().AUTH_API_URL);
  try {
    const objResponse = await fetch(objUrl, {
      method,
      headers: dicHeader,
      body: body === undefined ? undefined : JSON.stringify(body),
      cache: 'no-store',
    });
    // [Authorize] của backend trả 401 rỗng body khi token sai/hết hạn
    if (objResponse.status === 401) return failResult(401, 'Phiên đăng nhập đã hết hạn');
    const strBody = await objResponse.text();
    // Backend văng exception chưa bắt sẽ trả text/HTML thay vì APIResult → log rõ để biết lỗi gì
    if (!objResponse.headers.get('content-type')?.includes('application/json')) {
      console.error(`[auth-api] ${strPath} trả HTTP ${objResponse.status} không phải JSON:
${strBody.slice(0, 800)}`);
      return failResult(objResponse.status, 'Máy chủ đang gặp sự cố, vui lòng thử lại sau');
    }
    const objResult = JSON.parse(strBody) as ApiResult<T>;
    const strRetryAfter = objResponse.headers.get('Retry-After');
    return strRetryAfter ? { ...objResult, retryAfter: Number(strRetryAfter) } : objResult;
  } catch (error) {
    console.error(`[auth-api] Lỗi gọi ${strPath}`, error);
    return failResult(AUTH_ERROR_CODE.NetworkError, 'Không kết nối được máy chủ, vui lòng thử lại sau');
  }
}

/** Chuyển ApiResult của backend sang ActionResult gọn cho client */
export function toActionResult<T>(objResult: ApiResult<T>): ActionResult<T> {
  if (objResult.isError || objResult.resultObject === null) {
    return { ok: false, code: objResult.statusID, message: objResult.message, retryAfter: objResult.retryAfter };
  }
  return { ok: true, data: objResult.resultObject };
}
