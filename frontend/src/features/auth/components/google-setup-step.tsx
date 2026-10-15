'use client';

import { useState, useTransition } from 'react';

import { zodResolver } from '@hookform/resolvers/zod';
import { useForm, useWatch } from 'react-hook-form';

import { googleCompleteAction } from '../actions';
import { describeAuthError } from '../error-message';
import { PASSWORD_MAX_LENGTH, googleCompleteSchema } from '../schema';
import { AuthField } from './auth-field';
import { AuthSubmitButton } from './auth-submit-button';
import { FormError } from './form-error';
import { PasswordChecklist } from './password-checklist';
import { PasswordMatchMeter } from './password-match-meter';
import { TurnstileWidget } from './turnstile-widget';
import type { GoogleCompleteInput } from '../schema';

type GoogleSetupStepProps = { setupToken: string; onDone: (strOtpauthUri: string) => void };

/** Tài khoản mới tạo qua Google: đặt mật khẩu dự phòng rồi nhận khóa OTP */
export function GoogleSetupStep({ setupToken, onDone }: GoogleSetupStepProps) {
  const [strError, setStrError] = useState('');
  const [intAttempt, setIntAttempt] = useState(0);
  const [bolPending, startTransition] = useTransition();
  const { register, handleSubmit, setValue, control, formState } = useForm<GoogleCompleteInput>({
    resolver: zodResolver(googleCompleteSchema),
    defaultValues: { setupToken, newPassword: '', confirmPassword: '', captchaToken: '' },
  });
  const strPassword = useWatch({ control, name: 'newPassword' });
  const strConfirmPassword = useWatch({ control, name: 'confirmPassword' });

  const handleComplete = (objInput: GoogleCompleteInput) =>
    startTransition(async () => {
      const objResult = await googleCompleteAction(objInput);
      if (objResult.ok) return onDone(objResult.data.otpauthUri);
      setStrError(describeAuthError(objResult));
      setValue('captchaToken', '');
      setIntAttempt((x) => x + 1);
    });

  return (
    <form onSubmit={handleSubmit(handleComplete)} className="space-y-5" noValidate>
      <p className="text-sm text-slate-300">Tài khoản Google mới — đặt mật khẩu dự phòng để hoàn tất đăng ký.</p>
      <AuthField
        label="Mật khẩu"
        type="password"
        maxLength={PASSWORD_MAX_LENGTH}
        autoComplete="new-password"
        error={formState.errors.newPassword?.message}
        {...register('newPassword')}
      />
      <PasswordChecklist value={strPassword} />
      <AuthField
        label="Nhập lại mật khẩu"
        type="password"
        maxLength={PASSWORD_MAX_LENGTH}
        autoComplete="new-password"
        error={formState.errors.confirmPassword?.message}
        {...register('confirmPassword')}
      />
      <PasswordMatchMeter password={strPassword} confirm={strConfirmPassword} />
      <TurnstileWidget
        key={intAttempt}
        onToken={(strToken) => setValue('captchaToken', strToken, { shouldValidate: true })}
      />
      <FormError message={strError} />
      <AuthSubmitButton pending={bolPending}>Hoàn tất</AuthSubmitButton>
    </form>
  );
}
