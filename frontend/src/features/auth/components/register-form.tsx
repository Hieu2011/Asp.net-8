'use client';

import { useState, useTransition } from 'react';

import { zodResolver } from '@hookform/resolvers/zod';
import { useForm, useWatch } from 'react-hook-form';

import { registerAction } from '../actions';
import { describeAuthError } from '../error-message';
import { EMAIL_MAX_LENGTH, FULL_NAME_MAX_LENGTH, PASSWORD_MAX_LENGTH, registerSchema } from '../schema';
import { AuthField } from './auth-field';
import { AuthSubmitButton } from './auth-submit-button';
import { AuthSwitchLink } from './auth-switch-link';
import { FormError } from './form-error';
import { OtpSetupCard } from './otp-setup-card';
import { PasswordChecklist } from './password-checklist';
import { PasswordMatchMeter } from './password-match-meter';
import { RegisterVerifyStep } from './register-verify-step';
import { TurnstileWidget } from './turnstile-widget';
import type { RegisterInput } from '../schema';
import type { RegisterOtpSentResponse } from '../types';

/** Đăng ký 3 bước: điền thông tin → nhập mã gửi về email → quét QR thiết lập OTP */
export function RegisterForm() {
  const [objOtpSent, setObjOtpSent] = useState<RegisterOtpSentResponse | null>(null);
  const [strOtpauthUri, setStrOtpauthUri] = useState('');
  const [strError, setStrError] = useState('');
  const [intAttempt, setIntAttempt] = useState(0);
  const [bolPending, startTransition] = useTransition();
  const { register, handleSubmit, setValue, control, formState } = useForm<RegisterInput>({
    resolver: zodResolver(registerSchema),
    defaultValues: { fullName: '', email: '', password: '', confirmPassword: '', captchaToken: '' },
  });
  const strPassword = useWatch({ control, name: 'password' });
  const strConfirmPassword = useWatch({ control, name: 'confirmPassword' });

  // Token captcha chỉ dùng 1 lần → mỗi lần gửi xong (thành công hay lỗi) đều dựng widget mới
  const resetCaptcha = () => {
    setValue('captchaToken', '');
    setIntAttempt((x) => x + 1);
  };

  const handleRegister = (objInput: RegisterInput) =>
    startTransition(async () => {
      const objResult = await registerAction(objInput);
      resetCaptcha();
      if (!objResult.ok) return setStrError(describeAuthError(objResult));
      setStrError('');
      setObjOtpSent(objResult.data);
    });

  const handleBackToForm = (strMessage?: string) => {
    setObjOtpSent(null);
    setStrError(strMessage ?? '');
  };

  if (strOtpauthUri) return <OtpSetupCard otpauthUri={strOtpauthUri} title="Tạo tài khoản thành công" />;

  if (objOtpSent) {
    return (
      <div data-auth-split className="space-y-5">
        <div data-auth-split className="text-center">
          <h1 className="text-2xl font-semibold text-slate-50 sm:text-3xl">Xác thực email</h1>
          <p className="mt-1 text-sm text-slate-400">Bước cuối trước khi tạo tài khoản</p>
        </div>
        <RegisterVerifyStep
          registerToken={objOtpSent.registerToken}
          email={objOtpSent.email}
          resendAfter={objOtpSent.resendAfter}
          onVerified={setStrOtpauthUri}
          onBack={handleBackToForm}
        />
      </div>
    );
  }

  return (
    // data-wide: LampScene nới khung form rộng hơn để chia 2 cột, đỡ dài phải cuộn
    <div data-wide data-auth-split className="space-y-5">
      <div data-auth-split className="text-center">
        <h1 className="text-2xl font-semibold text-slate-50 sm:text-3xl">Tạo tài khoản</h1>
        <p className="mt-1 text-sm text-slate-400">Bảo vệ bằng mã OTP — không lo lộ mật khẩu</p>
      </div>
      <form onSubmit={handleSubmit(handleRegister)} className="space-y-4" noValidate>
        <div data-auth-split className="grid items-start gap-x-6 gap-y-4 sm:grid-cols-2">
          <AuthField
            label="Họ tên"
            maxLength={FULL_NAME_MAX_LENGTH}
            autoComplete="name"
            error={formState.errors.fullName?.message}
            {...register('fullName')}
          />
          <AuthField
            label="Email"
            type="email"
            maxLength={EMAIL_MAX_LENGTH}
            autoComplete="email"
            error={formState.errors.email?.message}
            {...register('email')}
          />
          <AuthField
            label="Mật khẩu"
            type="password"
            maxLength={PASSWORD_MAX_LENGTH}
            autoComplete="new-password"
            error={formState.errors.password?.message}
            {...register('password')}
          />
          <div className="space-y-4">
            <AuthField
              label="Nhập lại mật khẩu"
              type="password"
              maxLength={PASSWORD_MAX_LENGTH}
              autoComplete="new-password"
              error={formState.errors.confirmPassword?.message}
              {...register('confirmPassword')}
            />
            <PasswordMatchMeter password={strPassword} confirm={strConfirmPassword} />
          </div>
        </div>
        <PasswordChecklist value={strPassword} />
        <TurnstileWidget
          key={intAttempt}
          onToken={(strToken) => setValue('captchaToken', strToken, { shouldValidate: true })}
        />
        {formState.errors.captchaToken && (
          <p className="text-xs text-rose-300">{formState.errors.captchaToken.message}</p>
        )}
        <FormError message={strError} />
        <AuthSubmitButton pending={bolPending}>Đăng ký</AuthSubmitButton>
      </form>
      <p className="text-center text-sm text-slate-400">
        Đã có tài khoản?{' '}
        <AuthSwitchLink href="/login" className="font-medium text-amber-200 hover:text-amber-100">
          Đăng nhập
        </AuthSwitchLink>
      </p>
    </div>
  );
}
