'use client';

import { useState, useTransition } from 'react';

import { zodResolver } from '@hookform/resolvers/zod';
import { Controller, useForm } from 'react-hook-form';

import { loginWithOtpAction } from '../actions';
import { describeAuthError } from '../error-message';
import { EMAIL_MAX_LENGTH, loginOtpSchema } from '../schema';
import { AUTH_ERROR_CODE } from '../types';
import { AuthField } from './auth-field';
import { AuthSubmitButton } from './auth-submit-button';
import { FormError } from './form-error';
import { OtpInput } from './otp-input';
import { TurnstileWidget } from './turnstile-widget';
import type { LoginOtpInput } from '../schema';

const MAX_OTP_FAIL = 3;

type LoginOtpStepProps = {
  defaultEmail: string;
  onSuccess: () => void;
  onFallbackPassword: (strEmail: string) => void;
};

/** Đăng nhập chính: email + OTP. Sai ≥ 1 lần bắt captcha, sai 3 lần chuyển sang mật khẩu */
export function LoginOtpStep({ defaultEmail, onSuccess, onFallbackPassword }: LoginOtpStepProps) {
  const [intOtpFail, setIntOtpFail] = useState(0);
  const [bolNeedCaptcha, setBolNeedCaptcha] = useState(false);
  // Token captcha chỉ dùng 1 lần → lần gửi nào thất bại cũng phải dựng widget mới để lấy token mới
  const [intCaptchaKey, setIntCaptchaKey] = useState(0);
  const [strError, setStrError] = useState('');
  const [bolPending, startTransition] = useTransition();
  const { register, control, handleSubmit, setValue, formState } = useForm<LoginOtpInput>({
    resolver: zodResolver(loginOtpSchema),
    defaultValues: { email: defaultEmail, otp: '', captchaToken: '' },
  });

  const handleLogin = (objInput: LoginOtpInput) =>
    startTransition(async () => {
      const objResult = await loginWithOtpAction(objInput);
      if (objResult.ok) return onSuccess();
      setStrError(describeAuthError(objResult));
      setValue('otp', '');
      setValue('captchaToken', '');
      setIntCaptchaKey((x) => x + 1);
      if (objResult.code === AUTH_ERROR_CODE.OtpLocked) return onFallbackPassword(objInput.email);
      if (objResult.code === AUTH_ERROR_CODE.CaptchaRequired) return setBolNeedCaptcha(true);
      if (objResult.code !== AUTH_ERROR_CODE.InvalidCredential) return;
      const intFail = intOtpFail + 1;
      setIntOtpFail(intFail);
      setBolNeedCaptcha(true);
      if (intFail >= MAX_OTP_FAIL) onFallbackPassword(objInput.email);
    });

  return (
    <form onSubmit={handleSubmit(handleLogin)} className="space-y-4" noValidate>
      <AuthField
        label="Email"
        type="email"
        maxLength={EMAIL_MAX_LENGTH}
        autoComplete="email"
        error={formState.errors.email?.message}
        {...register('email')}
      />
      <Controller
        control={control}
        name="otp"
        render={({ field, fieldState }) => (
          <OtpInput
            label="Mã OTP trong ứng dụng xác thực"
            value={field.value}
            onChange={field.onChange}
            onBlur={field.onBlur}
            error={fieldState.error?.message}
          />
        )}
      />
      {bolNeedCaptcha && (
        <TurnstileWidget key={intCaptchaKey} onToken={(strToken) => setValue('captchaToken', strToken)} />
      )}
      {/* Gộp số lần sai vào chung hộp lỗi cho gọn — trang vừa 1 màn hình kể cả khi hiện captcha */}
      <FormError
        message={
          intOtpFail > 0
            ? `${strError} Sai ${intOtpFail}/${MAX_OTP_FAIL} lần — sai ${MAX_OTP_FAIL} lần sẽ chuyển sang đăng nhập bằng mật khẩu.`
            : strError
        }
      />
      <AuthSubmitButton pending={bolPending}>Đăng nhập</AuthSubmitButton>
    </form>
  );
}
