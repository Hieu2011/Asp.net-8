'use server';

import { revalidatePath } from 'next/cache';

import { z } from 'zod';

import { AUTH_ERROR_CODE } from '@/features/auth/types';
import type { ActionResult } from '@/features/auth/types';
import { callAuthApi, toActionResult } from '@/lib/auth/auth-api';
import { ROLE_LEVEL } from '@/lib/auth/roles';
import { getAccessToken } from '@/lib/auth/session';

import type { AdminSession } from './types';

type SuccessResponse = { success: boolean };
type UserCommand = 'disable' | 'enable' | 'unlock' | 'delete' | 'revoke-sessions';

const COMMAND_REQUEST: Record<UserCommand, { method: 'PUT' | 'DELETE'; suffix: string }> = {
  disable: { method: 'PUT', suffix: '/disable' },
  enable: { method: 'PUT', suffix: '/enable' },
  unlock: { method: 'PUT', suffix: '/unlock' },
  delete: { method: 'DELETE', suffix: '' },
  'revoke-sessions': { method: 'DELETE', suffix: '/sessions' },
};

const invalid = (strMessage: string): ActionResult<never> => ({
  ok: false,
  code: AUTH_ERROR_CODE.ValidationError,
  message: strMessage,
});

const notSignedIn: ActionResult<never> = {
  ok: false,
  code: 401,
  message: 'Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại',
};

const isUuid = (strValue: string) => z.uuid().safeParse(strValue).success;

// Server Action là endpoint công khai → validate input; quyền Admin do AuthService kiểm tra ([RequireMinRoleLevel])
async function sendAdminRequest<T>(strPath: string, strMethod: 'GET' | 'PUT' | 'DELETE', objBody?: unknown) {
  const strAccessToken = await getAccessToken();
  if (!strAccessToken) return notSignedIn;
  return toActionResult(
    await callAuthApi<T>(strPath, { method: strMethod, body: objBody, accessToken: strAccessToken }),
  );
}

export async function runUserCommandAction(
  strUserID: string,
  strCommand: UserCommand,
): Promise<ActionResult<SuccessResponse>> {
  const objRequest = COMMAND_REQUEST[strCommand];
  if (!isUuid(strUserID) || !objRequest) return invalid('Yêu cầu không hợp lệ');
  const objResult = await sendAdminRequest<SuccessResponse>(
    `/auth/users/${strUserID}${objRequest.suffix}`,
    objRequest.method,
  );
  revalidatePath('/admin', 'layout');
  return objResult;
}

export async function changeUserRoleAction(
  strUserID: string,
  intRoleLevel: number,
): Promise<ActionResult<SuccessResponse>> {
  const bolValidRole = (Object.values(ROLE_LEVEL) as number[]).includes(intRoleLevel);
  if (!isUuid(strUserID) || !bolValidRole) return invalid('Vai trò không hợp lệ');
  const objResult = await sendAdminRequest<SuccessResponse>(`/auth/users/${strUserID}/role`, 'PUT', {
    newRoleLevel: intRoleLevel,
  });
  revalidatePath('/admin', 'layout');
  return objResult;
}

export async function getUserSessionsAction(strUserID: string): Promise<ActionResult<AdminSession[]>> {
  if (!isUuid(strUserID)) return invalid('Người dùng không hợp lệ');
  return sendAdminRequest<AdminSession[]>(`/auth/users/${strUserID}/sessions`, 'GET');
}

export async function revokeUserSessionAction(
  strUserID: string,
  strSessionID: string,
): Promise<ActionResult<SuccessResponse>> {
  if (!isUuid(strUserID) || !isUuid(strSessionID)) return invalid('Phiên không hợp lệ');
  const objResult = await sendAdminRequest<SuccessResponse>(
    `/auth/users/${strUserID}/sessions/${strSessionID}`,
    'DELETE',
  );
  revalidatePath('/admin', 'layout');
  return objResult;
}
