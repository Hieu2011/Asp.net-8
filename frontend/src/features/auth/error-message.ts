import type { ActionResult } from './types';

type FailResult = Extract<ActionResult, { ok: false }>;

/** Thông báo lỗi hiển thị cho người dùng — kèm thời gian chờ khi bị khóa tạm */
export function describeAuthError(objFail: FailResult): string {
  if (!objFail.retryAfter) return objFail.message;
  const intMinute = Math.max(1, Math.ceil(objFail.retryAfter / 60));
  return `${objFail.message} Vui lòng thử lại sau ${intMinute} phút.`;
}
