'use client';

import { useRouter } from 'next/navigation';
import { useState, useTransition } from 'react';

import { KeyRound, QrCode } from 'lucide-react';

import { cn } from '@/lib/utils';

import { googleLoginAction } from '../actions';
import { AuthSwitchLink } from './auth-switch-link';
import { describeAuthError } from '../error-message';
import { FormError } from './form-error';
import { GoogleSetupStep } from './google-setup-step';
import { GoogleSignInButton } from './google-sign-in-button';
import { LoginOtpStep } from './login-otp-step';
import { LoginPasswordStep } from './login-password-step';
import { OtpSetupCard } from './otp-setup-card';
import { QrLoginPanel } from './qr-login-panel';

type LoginFormProps = { nextPath: string };
type LoginStep = 'otp' | 'qr' | 'password' | 'google-setup' | 'otp-setup';

const LOGIN_MODES = [
  { step: 'otp', label: 'Mã OTP', icon: KeyRound },
  { step: 'qr', label: 'Quét mã QR', icon: QrCode },
] as const;

export function LoginForm({ nextPath }: LoginFormProps) {
  const objRouter = useRouter();
  const [strStep, setStrStep] = useState<LoginStep>('otp');
  const [strEmail, setStrEmail] = useState('');
  const [strSetupToken, setStrSetupToken] = useState('');
  const [strOtpauthUri, setStrOtpauthUri] = useState('');
  const [strError, setStrError] = useState('');
  const [bolGooglePending, startGoogleTransition] = useTransition();

  const handleSuccess = () => {
    objRouter.replace(nextPath);
    objRouter.refresh();
  };

  const handleFallbackPassword = (strValue: string) => {
    setStrEmail(strValue);
    setStrStep('password');
  };

  // Google chỉ nhận diện người dùng — tài khoản cũ vẫn phải nhập OTP, tài khoản mới phải đặt mật khẩu
  const handleGoogleCredential = (strIdToken: string) =>
    startGoogleTransition(async () => {
      const objResult = await googleLoginAction(strIdToken);
      if (!objResult.ok) return setStrError(describeAuthError(objResult));
      setStrError('');
      if (objResult.data.requiresSetup && objResult.data.setupToken) {
        setStrSetupToken(objResult.data.setupToken);
        return setStrStep('google-setup');
      }
      setStrEmail(objResult.data.email ?? '');
      setStrStep('otp');
    });

  const handleGoogleDone = (strUri: string) => {
    setStrOtpauthUri(strUri);
    setStrStep('otp-setup');
  };

  // Điền sẵn email từ nhãn otpauth://totp/<Issuer>:<email> để đăng nhập ngay
  const handleSetupContinue = () => {
    const strLabel = decodeURIComponent(strOtpauthUri.split('?')[0]?.split('/').pop() ?? '');
    setStrEmail(strLabel.slice(strLabel.indexOf(':') + 1));
    setStrOtpauthUri('');
    setStrStep('otp');
  };

  if (strStep === 'otp-setup')
    return <OtpSetupCard otpauthUri={strOtpauthUri} title="Hoàn tất đăng ký" onContinue={handleSetupContinue} />;

  return (
    <div data-auth-split className="space-y-4 tall:space-y-5">
      <div data-auth-split className="text-center">
        <h1 className="text-2xl font-semibold text-slate-50 sm:text-3xl">Chào mừng trở lại</h1>
        <p className="mt-1 text-sm text-slate-400">
          {strStep === 'qr' ? 'Quét mã bằng điện thoại đã đăng nhập' : 'Đăng nhập bằng mã OTP trong ứng dụng xác thực'}
        </p>
      </div>

      {(strStep === 'otp' || strStep === 'qr') && (
        <div role="tablist" aria-label="Cách đăng nhập" className="grid grid-cols-2 gap-1 rounded-xl bg-white/5 p-1">
          {LOGIN_MODES.map((x) => (
            <button
              key={x.step}
              type="button"
              role="tab"
              aria-selected={strStep === x.step}
              onClick={() => {
                setStrError('');
                setStrStep(x.step);
              }}
              className={cn(
                'flex h-9 items-center justify-center gap-2 rounded-lg text-sm transition-colors',
                strStep === x.step ? 'bg-amber-200 font-medium text-slate-900' : 'text-slate-300 hover:text-white',
              )}
            >
              <x.icon aria-hidden className="size-4" />
              {x.label}
            </button>
          ))}
        </div>
      )}

      {strStep === 'otp' && (
        <LoginOtpStep
          key={strEmail}
          defaultEmail={strEmail}
          onSuccess={handleSuccess}
          onFallbackPassword={handleFallbackPassword}
        />
      )}
      {strStep === 'password' && (
        <LoginPasswordStep email={strEmail} onSuccess={handleSuccess} onBack={() => setStrStep('otp')} />
      )}
      {strStep === 'qr' && <QrLoginPanel onSuccess={handleSuccess} />}
      {strStep === 'google-setup' && <GoogleSetupStep setupToken={strSetupToken} onDone={handleGoogleDone} />}

      {strStep === 'otp' && (
        <>
          <div className="flex items-center gap-3 text-xs text-slate-500">
            <span className="h-px flex-1 bg-slate-700" />
            hoặc
            <span className="h-px flex-1 bg-slate-700" />
          </div>
          <GoogleSignInButton onCredential={handleGoogleCredential} />
          {bolGooglePending && <p className="text-center text-xs text-slate-400">Đang xác minh Google…</p>}
        </>
      )}
      <FormError message={strError} />

      <div className="flex justify-between text-sm">
        <AuthSwitchLink href="/forgot-password" className="text-slate-400 hover:text-amber-100">
          Quên mật khẩu / mất OTP?
        </AuthSwitchLink>
        <AuthSwitchLink href="/register" className="font-medium text-amber-200 hover:text-amber-100">
          Tạo tài khoản
        </AuthSwitchLink>
      </div>
    </div>
  );
}
