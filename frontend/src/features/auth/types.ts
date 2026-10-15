// Hợp đồng JSON với AuthService (ASP.NET trả camelCase) — giữ đúng tên field backend

export type ApiResult<T> = {
  isError: boolean;
  statusID: number;
  message: string;
  messageDetail: string;
  resultObject: T | null;
  /** Giây phải chờ khi bị khóa tạm (header Retry-After) */
  retryAfter?: number;
};

export type TokenResponse = { accessToken: string; refreshToken: string; expiresIn: number };

export type OtpauthResponse = { otpauthUri: string };

export type GoogleLoginResponse = {
  requiresSetup: boolean;
  setupToken: string | null;
  requiresOtp: boolean;
  email: string | null;
};

export type RegisterOtpSentResponse = { registerToken: string; email: string; expiresIn: number; resendAfter: number };

export type VerifyResetOtpResponse = { resetToken: string };

export type QrStartResponse = { challengeId: string; pollSecret: string; expiresIn: number; pollInterval: number };

export type QrLoginStatus = 'pending' | 'scanned' | 'approved' | 'rejected' | 'expired';

export type QrPollResponse = { status: QrLoginStatus; token: TokenResponse | null };

export type QrChallengeInfo = {
  status: QrLoginStatus;
  deviceInfo: string | null;
  ipAddress: string | null;
  createdAt: string;
};

export type MessageResponse = { message: string };

export type UserProfile = {
  id: string;
  username: string;
  email: string;
  fullName: string | null;
  roleLevel: number;
  isActive: boolean;
  isTotpEnabled: boolean;
  hasGoogleLinked: boolean;
  createdDate: string;
};

export type SessionInfo = {
  id: string;
  deviceInfo: string | null;
  ipAddress: string | null;
  lastActivityAt: string;
  expiresAt: string;
  isRevoked: boolean;
  createdDate: string;
};

/** Mã lỗi khớp enum AuthErrorCode của backend */
export const AUTH_ERROR_CODE = {
  NetworkError: -1,
  ValidationError: 0,
  InvalidCredential: 1,
  OtpLocked: 2,
  AccountLocked: 3,
  AccountPermanentlyLocked: 4,
  OtpFallbackNotAllowed: 5,
  CaptchaRequired: 6,
  CaptchaInvalid: 7,
  PasswordPolicyViolation: 8,
  ResetOtpInvalid: 9,
  ResetOtpExpired: 10,
  ResetTokenInvalid: 11,
  RefreshTokenInvalid: 12,
  RefreshTokenReuseDetected: 13,
  GoogleTokenInvalid: 17,
  GoogleEmailNotVerified: 18,
  GoogleSetupTokenInvalid: 19,
  EmailAlreadyExists: 24,
  ResetOtpCooldownActive: 25,
  EmailInvalid: 27,
  FullNameInvalid: 28,
  RegisterTokenInvalid: 29,
  RegisterOtpInvalid: 30,
  RegisterOtpCooldown: 31,
  QrChallengeInvalid: 32,
  AccountDisabled: 34,
  RoleInvalid: 35,
  TooManyRequests: 36,
  RefreshTokenConcurrent: 37,
} as const;

/** Kết quả trả về từ Server Action cho client */
export type ActionResult<T = null> =
  { ok: true; data: T } | { ok: false; code: number; message: string; retryAfter?: number };
