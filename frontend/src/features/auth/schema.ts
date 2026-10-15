import { z } from 'zod';

// Khớp PasswordPolicy/FullNamePolicy/EmailPolicy backend
export const PASSWORD_MIN_LENGTH = 8;
export const PASSWORD_MAX_LENGTH = 24;
export const FULL_NAME_MAX_LENGTH = 100;
export const EMAIL_MAX_LENGTH = 254;
const PASSWORD_PATTERN = /^(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,24}$/;
const FULL_NAME_PATTERN = /^[\p{L}\p{M}][\p{L}\p{M}' .-]*$/u;

/** Từng điều kiện mật khẩu — hiện checklist tick dần khi người dùng gõ */
export const PASSWORD_RULES = [
  {
    label: `${PASSWORD_MIN_LENGTH}–${PASSWORD_MAX_LENGTH} ký tự`,
    test: (strValue: string) => strValue.length >= PASSWORD_MIN_LENGTH && strValue.length <= PASSWORD_MAX_LENGTH,
  },
  { label: 'Có chữ hoa', test: (strValue: string) => /[A-Z]/.test(strValue) },
  { label: 'Có chữ số', test: (strValue: string) => /\d/.test(strValue) },
  { label: 'Có ký tự đặc biệt', test: (strValue: string) => /[^a-zA-Z0-9]/.test(strValue) },
] as const;

/** Họ tên: gộp khoảng trắng thừa giống backend rồi mới kiểm tra */
export const fullNameSchema = z
  .string()
  .overwrite((x) => x.replace(/\s+/g, ' ').trim())
  .min(1, 'Vui lòng nhập họ tên')
  .min(2, 'Họ tên tối thiểu 2 ký tự')
  .max(FULL_NAME_MAX_LENGTH, `Họ tên tối đa ${FULL_NAME_MAX_LENGTH} ký tự`)
  .regex(FULL_NAME_PATTERN, 'Họ tên chỉ gồm chữ và khoảng trắng');

export const otpSchema = z
  .string()
  .min(1, 'Vui lòng nhập mã OTP')
  .regex(/^\d{6}$/, 'Mã OTP phải đủ 6 chữ số');

export const emailSchema = z
  .string()
  .trim()
  .min(1, 'Vui lòng nhập email')
  .max(EMAIL_MAX_LENGTH, 'Email quá dài')
  .pipe(z.email('Email không hợp lệ'));

export const passwordSchema = z
  .string()
  .min(1, 'Vui lòng nhập mật khẩu')
  .regex(PASSWORD_PATTERN, 'Mật khẩu 8-24 ký tự, có chữ hoa, chữ số và ký tự đặc biệt');

const captchaSchema = z.string().min(1, 'Vui lòng xác thực bạn không phải robot');

export const loginOtpSchema = z.object({
  email: emailSchema,
  otp: otpSchema,
  captchaToken: z.string().optional(),
});

export const loginPasswordSchema = z.object({
  email: emailSchema,
  password: z.string().min(1, 'Vui lòng nhập mật khẩu'),
  captchaToken: captchaSchema,
});

export const registerSchema = z
  .object({
    fullName: fullNameSchema,
    email: emailSchema,
    password: passwordSchema,
    confirmPassword: z.string().min(1, 'Vui lòng nhập lại mật khẩu'),
    captchaToken: captchaSchema,
  })
  .refine((x) => x.password === x.confirmPassword, {
    message: 'Mật khẩu nhập lại không khớp',
    path: ['confirmPassword'],
  });

export const registerVerifySchema = z.object({ registerToken: z.string().min(1), otp: otpSchema });

export const forgotPasswordSchema = z.object({ email: emailSchema, captchaToken: captchaSchema });

export const verifyResetOtpSchema = z.object({ email: emailSchema, otp: otpSchema });

export const resetPasswordSchema = z
  .object({ resetToken: z.string().min(1), newPassword: passwordSchema, confirmPassword: z.string() })
  .refine((x) => x.newPassword === x.confirmPassword, {
    message: 'Mật khẩu nhập lại không khớp',
    path: ['confirmPassword'],
  });

export const googleCompleteSchema = z
  .object({
    setupToken: z.string().min(1),
    newPassword: passwordSchema,
    confirmPassword: z.string(),
    captchaToken: captchaSchema,
  })
  .refine((x) => x.newPassword === x.confirmPassword, {
    message: 'Mật khẩu nhập lại không khớp',
    path: ['confirmPassword'],
  });

export type LoginOtpInput = z.infer<typeof loginOtpSchema>;
export type LoginPasswordInput = z.infer<typeof loginPasswordSchema>;
export type RegisterInput = z.infer<typeof registerSchema>;
export type RegisterVerifyInput = z.infer<typeof registerVerifySchema>;
export type ForgotPasswordInput = z.infer<typeof forgotPasswordSchema>;
export type VerifyResetOtpInput = z.infer<typeof verifyResetOtpSchema>;
export type ResetPasswordInput = z.infer<typeof resetPasswordSchema>;
export type GoogleCompleteInput = z.infer<typeof googleCompleteSchema>;
