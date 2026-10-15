'use server';

import { cookies } from 'next/headers';

import type { z } from 'zod';

import { callAuthApi, toActionResult } from '@/lib/auth/auth-api';
import { buildAuthCookies, buildClearedAuthCookies } from '@/lib/auth/auth-cookies';
import { getAccessToken } from '@/lib/auth/session';

import {
  forgotPasswordSchema,
  googleCompleteSchema,
  loginOtpSchema,
  loginPasswordSchema,
  registerSchema,
  registerVerifySchema,
  resetPasswordSchema,
  verifyResetOtpSchema,
} from './schema';
import type {
  ForgotPasswordInput,
  GoogleCompleteInput,
  LoginOtpInput,
  LoginPasswordInput,
  RegisterInput,
  RegisterVerifyInput,
  ResetPasswordInput,
  VerifyResetOtpInput,
} from './schema';
import { AUTH_ERROR_CODE } from './types';
import type {
  ActionResult,
  GoogleLoginResponse,
  MessageResponse,
  OtpauthResponse,
  RegisterOtpSentResponse,
  TokenResponse,
  VerifyResetOtpResponse,
} from './types';

// Server Action là endpoint công khai → luôn validate lại input phía server
const invalidInput = (objError: z.ZodError): ActionResult<never> => ({
  ok: false,
  code: AUTH_ERROR_CODE.ValidationError,
  message: objError.issues[0]?.message ?? 'Dữ liệu không hợp lệ',
});

async function signIn(strPath: string, objBody: unknown): Promise<ActionResult> {
  const objResult = toActionResult(await callAuthApi<TokenResponse>(strPath, { body: objBody }));
  if (!objResult.ok) return objResult;
  const objCookieStore = await cookies();
  for (const objCookie of buildAuthCookies(objResult.data))
    objCookieStore.set(objCookie.name, objCookie.value, objCookie.options);
  return { ok: true, data: null };
}

export async function loginWithOtpAction(objInput: LoginOtpInput): Promise<ActionResult> {
  const objParsed = loginOtpSchema.safeParse(objInput);
  return objParsed.success ? signIn('/auth/login', objParsed.data) : invalidInput(objParsed.error);
}

export async function loginWithPasswordAction(objInput: LoginPasswordInput): Promise<ActionResult> {
  const objParsed = loginPasswordSchema.safeParse(objInput);
  return objParsed.success ? signIn('/auth/login/password', objParsed.data) : invalidInput(objParsed.error);
}

/** Bước 1: gửi mã xác thực về email — chưa tạo tài khoản */
export async function registerAction(objInput: RegisterInput): Promise<ActionResult<RegisterOtpSentResponse>> {
  const objParsed = registerSchema.safeParse(objInput);
  if (!objParsed.success) return invalidInput(objParsed.error);
  const { confirmPassword: _confirm, ...objBody } = objParsed.data;
  return toActionResult(await callAuthApi<RegisterOtpSentResponse>('/auth/register', { body: objBody }));
}

/** Bước 2: đúng mã mới tạo tài khoản, trả otpauth URI để quét QR */
export async function verifyRegisterAction(objInput: RegisterVerifyInput): Promise<ActionResult<OtpauthResponse>> {
  const objParsed = registerVerifySchema.safeParse(objInput);
  if (!objParsed.success) return invalidInput(objParsed.error);
  return toActionResult(await callAuthApi<OtpauthResponse>('/auth/register/verify', { body: objParsed.data }));
}

export async function resendRegisterOtpAction(
  strRegisterToken: string,
): Promise<ActionResult<RegisterOtpSentResponse>> {
  if (!strRegisterToken)
    return { ok: false, code: AUTH_ERROR_CODE.RegisterTokenInvalid, message: 'Phiên đăng ký đã hết hạn' };
  return toActionResult(
    await callAuthApi<RegisterOtpSentResponse>('/auth/register/resend', { body: { registerToken: strRegisterToken } }),
  );
}

export async function forgotPasswordAction(objInput: ForgotPasswordInput): Promise<ActionResult<MessageResponse>> {
  const objParsed = forgotPasswordSchema.safeParse(objInput);
  if (!objParsed.success) return invalidInput(objParsed.error);
  return toActionResult(await callAuthApi<MessageResponse>('/auth/password/forgot', { body: objParsed.data }));
}

export async function verifyResetOtpAction(
  objInput: VerifyResetOtpInput,
): Promise<ActionResult<VerifyResetOtpResponse>> {
  const objParsed = verifyResetOtpSchema.safeParse(objInput);
  if (!objParsed.success) return invalidInput(objParsed.error);
  return toActionResult(
    await callAuthApi<VerifyResetOtpResponse>('/auth/password/verify-reset-otp', { body: objParsed.data }),
  );
}

export async function resetPasswordAction(objInput: ResetPasswordInput): Promise<ActionResult<OtpauthResponse>> {
  const objParsed = resetPasswordSchema.safeParse(objInput);
  if (!objParsed.success) return invalidInput(objParsed.error);
  const { resetToken, newPassword } = objParsed.data;
  return toActionResult(
    await callAuthApi<OtpauthResponse>('/auth/password/reset', { body: { resetToken, newPassword } }),
  );
}

export async function googleLoginAction(strIdToken: string): Promise<ActionResult<GoogleLoginResponse>> {
  if (!strIdToken) return { ok: false, code: AUTH_ERROR_CODE.GoogleTokenInvalid, message: 'Thiếu Google ID Token' };
  return toActionResult(
    await callAuthApi<GoogleLoginResponse>('/auth/login/google', { body: { idToken: strIdToken } }),
  );
}

export async function googleCompleteAction(objInput: GoogleCompleteInput): Promise<ActionResult<OtpauthResponse>> {
  const objParsed = googleCompleteSchema.safeParse(objInput);
  if (!objParsed.success) return invalidInput(objParsed.error);
  const { setupToken, newPassword, captchaToken } = objParsed.data;
  return toActionResult(
    await callAuthApi<OtpauthResponse>('/auth/register/google/complete', {
      body: { setupToken, newPassword, captchaToken },
    }),
  );
}

/** Thu hồi phiên phía backend rồi mới xóa cookie — backend lỗi thì giữ nguyên đăng nhập để người dùng thử lại */
export async function logoutAction(): Promise<ActionResult<MessageResponse | null>> {
  const strAccessToken = await getAccessToken();
  if (strAccessToken) {
    const objResult = toActionResult(
      await callAuthApi<MessageResponse>('/auth/logout', { accessToken: strAccessToken }),
    );
    // 401 = phiên đã hết hạn/bị thu hồi → coi như đã đăng xuất
    if (!objResult.ok && objResult.code !== 401) return objResult;
  }
  const objCookieStore = await cookies();
  for (const objCookie of buildClearedAuthCookies())
    objCookieStore.set(objCookie.name, objCookie.value, objCookie.options);
  return { ok: true, data: null };
}
