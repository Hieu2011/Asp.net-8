'use client';

import { useState, useTransition } from 'react';

import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';

import { loginWithPasswordAction } from '../actions';
import { describeAuthError } from '../error-message';
import { PASSWORD_MAX_LENGTH, loginPasswordSchema } from '../schema';
import { AuthField } from './auth-field';
import { AuthSubmitButton } from './auth-submit-button';
import { FormError } from './form-error';
import { TurnstileWidget } from './turnstile-widget';
import type { LoginPasswordInput } from '../schema';

type LoginPasswordStepProps = { email: string; onSuccess: () => void; onBack: () => void };

/** Đăng nhập dự phòng bằng mật khẩu — backend chỉ cho khi đã sai OTP 3 lần, luôn bắt captcha */
export function LoginPasswordStep({ email, onSuccess, onBack }: LoginPasswordStepProps) {
  const [strError, setStrError] = useState('');
  const [intAttempt, setIntAttempt] = useState(0);
  const [bolPending, startTransition] = useTransition();
  const { register, handleSubmit, setValue, formState } = useForm<LoginPasswordInput>({
    resolver: zodResolver(loginPasswordSchema),
    defaultValues: { email, password: '', captchaToken: '' },
  });

  const handleLogin = (objInput: LoginPasswordInput) =>
    startTransition(async () => {
      const objResult = await loginWithPasswordAction(objInput);
      if (objResult.ok) return onSuccess();
      setStrError(describeAuthError(objResult));
      setValue('password', '');
      setValue('captchaToken', '');
      setIntAttempt((x) => x + 1);
    });

  return (
    <form onSubmit={handleSubmit(handleLogin)} className="space-y-5" noValidate>
      <p className="text-sm text-slate-300">
        Đăng nhập bằng mật khẩu cho tài khoản <span className="font-semibold break-all text-amber-100">{email}</span>
      </p>
      <AuthField
        label="Mật khẩu"
        type="password"
        maxLength={PASSWORD_MAX_LENGTH}
        autoComplete="current-password"
        error={formState.errors.password?.message}
        {...register('password')}
      />
      <TurnstileWidget
        key={intAttempt}
        onToken={(strToken) => setValue('captchaToken', strToken, { shouldValidate: true })}
      />
      {formState.errors.captchaToken && (
        <p className="text-xs text-rose-300">{formState.errors.captchaToken.message}</p>
      )}
      <FormError message={strError} />
      <AuthSubmitButton pending={bolPending}>Đăng nhập</AuthSubmitButton>
      <button type="button" onClick={onBack} className="w-full text-sm text-slate-400 hover:text-amber-100">
        Quay lại đăng nhập bằng OTP
      </button>
    </form>
  );
}
