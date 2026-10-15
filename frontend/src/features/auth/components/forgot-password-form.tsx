'use client';

import { useState, useTransition } from 'react';

import { zodResolver } from '@hookform/resolvers/zod';
import { Controller, useForm, useWatch } from 'react-hook-form';

import { forgotPasswordAction, resetPasswordAction, verifyResetOtpAction } from '../actions';
import { describeAuthError } from '../error-message';
import {
  EMAIL_MAX_LENGTH,
  PASSWORD_MAX_LENGTH,
  forgotPasswordSchema,
  resetPasswordSchema,
  verifyResetOtpSchema,
} from '../schema';
import { AuthField } from './auth-field';
import { AuthSubmitButton } from './auth-submit-button';
import { AuthSwitchLink } from './auth-switch-link';
import { FormError } from './form-error';
import { OtpInput } from './otp-input';
import { OtpSetupCard } from './otp-setup-card';
import { PasswordChecklist } from './password-checklist';
import { PasswordMatchMeter } from './password-match-meter';
import { TurnstileWidget } from './turnstile-widget';
import type { ForgotPasswordInput, ResetPasswordInput, VerifyResetOtpInput } from '../schema';

type RecoveryStep = 'request' | 'verify' | 'reset' | 'done';

/** Quên mật khẩu / mất OTP: gửi mã qua email → xác minh → đặt mật khẩu mới + khóa OTP mới */
export function ForgotPasswordForm() {
  const [strStep, setStrStep] = useState<RecoveryStep>('request');
  const [strEmail, setStrEmail] = useState('');
  const [strOtpauthUri, setStrOtpauthUri] = useState('');
  const [strInfo, setStrInfo] = useState('');
  const [strError, setStrError] = useState('');
  const [intAttempt, setIntAttempt] = useState(0);
  const [bolPending, startTransition] = useTransition();
  const objRequestForm = useForm<ForgotPasswordInput>({
    resolver: zodResolver(forgotPasswordSchema),
    defaultValues: { email: '', captchaToken: '' },
  });
  const objVerifyForm = useForm<VerifyResetOtpInput>({
    resolver: zodResolver(verifyResetOtpSchema),
    defaultValues: { email: '', otp: '' },
  });
  const objResetForm = useForm<ResetPasswordInput>({
    resolver: zodResolver(resetPasswordSchema),
    defaultValues: { resetToken: '', newPassword: '', confirmPassword: '' },
  });
  // Destructure formState để RHF theo dõi errors — đọc qua objXxxForm.formState bị React Compiler memo hóa, lỗi không hiện
  const { formState: objRequestState } = objRequestForm;
  const { formState: objResetState } = objResetForm;
  const strNewPassword = useWatch({ control: objResetForm.control, name: 'newPassword' });
  const strConfirmPassword = useWatch({ control: objResetForm.control, name: 'confirmPassword' });

  const handleRequest = (objInput: ForgotPasswordInput) =>
    startTransition(async () => {
      const objResult = await forgotPasswordAction(objInput);
      if (!objResult.ok) {
        setStrError(describeAuthError(objResult));
        objRequestForm.setValue('captchaToken', '');
        return setIntAttempt((x) => x + 1);
      }
      setStrError('');
      setStrInfo(objResult.data.message);
      setStrEmail(objInput.email);
      objVerifyForm.setValue('email', objInput.email);
      setStrStep('verify');
    });

  const handleVerify = (objInput: VerifyResetOtpInput) =>
    startTransition(async () => {
      const objResult = await verifyResetOtpAction(objInput);
      if (!objResult.ok) return setStrError(describeAuthError(objResult));
      setStrError('');
      objResetForm.setValue('resetToken', objResult.data.resetToken);
      setStrStep('reset');
    });

  const handleReset = (objInput: ResetPasswordInput) =>
    startTransition(async () => {
      const objResult = await resetPasswordAction(objInput);
      if (!objResult.ok) return setStrError(describeAuthError(objResult));
      setStrOtpauthUri(objResult.data.otpauthUri);
      setStrStep('done');
    });

  if (strStep === 'done') return <OtpSetupCard otpauthUri={strOtpauthUri} title="Đặt lại thành công" />;

  return (
    <div data-auth-split className="space-y-5">
      <div data-auth-split className="text-center">
        <h1 className="text-2xl font-semibold text-slate-50 sm:text-3xl">Khôi phục tài khoản</h1>
        <p className="mt-1 text-sm text-slate-400">Mã xác minh gửi về email đã đăng ký</p>
      </div>

      {strStep === 'request' && (
        <form onSubmit={objRequestForm.handleSubmit(handleRequest)} className="space-y-5" noValidate>
          <AuthField
            label="Email đã đăng ký"
            type="email"
            maxLength={EMAIL_MAX_LENGTH}
            autoComplete="email"
            error={objRequestState.errors.email?.message}
            {...objRequestForm.register('email')}
          />
          <TurnstileWidget
            key={intAttempt}
            onToken={(strToken) => objRequestForm.setValue('captchaToken', strToken, { shouldValidate: true })}
          />
          {objRequestState.errors.captchaToken && (
            <p className="text-xs text-rose-300">{objRequestState.errors.captchaToken.message}</p>
          )}
          <FormError message={strError} />
          <AuthSubmitButton pending={bolPending}>Gửi mã xác minh</AuthSubmitButton>
        </form>
      )}

      {strStep === 'verify' && (
        <form onSubmit={objVerifyForm.handleSubmit(handleVerify)} className="space-y-5" noValidate>
          <p className="text-sm text-slate-300">{strInfo}</p>
          <Controller
            control={objVerifyForm.control}
            name="otp"
            render={({ field, fieldState }) => (
              <OtpInput
                label={`Mã 6 số gửi tới ${strEmail}`}
                value={field.value}
                onChange={field.onChange}
                onBlur={field.onBlur}
                error={fieldState.error?.message}
              />
            )}
          />
          <FormError message={strError} />
          <AuthSubmitButton pending={bolPending}>Xác minh</AuthSubmitButton>
        </form>
      )}

      {strStep === 'reset' && (
        <form onSubmit={objResetForm.handleSubmit(handleReset)} className="space-y-5" noValidate>
          <AuthField
            label="Mật khẩu mới"
            type="password"
            maxLength={PASSWORD_MAX_LENGTH}
            autoComplete="new-password"
            error={objResetState.errors.newPassword?.message}
            {...objResetForm.register('newPassword')}
          />
          <PasswordChecklist value={strNewPassword} />
          <AuthField
            label="Nhập lại mật khẩu mới"
            type="password"
            maxLength={PASSWORD_MAX_LENGTH}
            autoComplete="new-password"
            error={objResetState.errors.confirmPassword?.message}
            {...objResetForm.register('confirmPassword')}
          />
          <PasswordMatchMeter password={strNewPassword} confirm={strConfirmPassword} />
          <p className="text-xs text-slate-400">Sau khi đặt lại, khóa OTP cũ hết hiệu lực — bạn sẽ nhận khóa mới.</p>
          <FormError message={strError} />
          <AuthSubmitButton pending={bolPending}>Đặt lại mật khẩu</AuthSubmitButton>
        </form>
      )}

      <p className="text-center text-sm">
        <AuthSwitchLink href="/login" className="text-slate-400 hover:text-amber-100">
          Quay lại đăng nhập
        </AuthSwitchLink>
      </p>
    </div>
  );
}
