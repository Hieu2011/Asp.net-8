'use client';

import { useEffect, useState, useTransition } from 'react';

import { zodResolver } from '@hookform/resolvers/zod';
import { Controller, useForm } from 'react-hook-form';

import { resendRegisterOtpAction, verifyRegisterAction } from '../actions';
import { describeAuthError } from '../error-message';
import { registerVerifySchema } from '../schema';
import { AUTH_ERROR_CODE } from '../types';
import { AuthSubmitButton } from './auth-submit-button';
import { FormError } from './form-error';
import { OtpInput } from './otp-input';
import type { RegisterVerifyInput } from '../schema';

type RegisterVerifyStepProps = {
  registerToken: string;
  email: string;
  resendAfter: number;
  onVerified: (strOtpauthUri: string) => void;
  onBack: (strMessage?: string) => void;
};

/** Bước 2 đăng ký: nhập mã 6 số gửi về email, gửi lại có đếm ngược */
export function RegisterVerifyStep({ registerToken, email, resendAfter, onVerified, onBack }: RegisterVerifyStepProps) {
  const [intResendLeft, setIntResendLeft] = useState(resendAfter);
  const [strError, setStrError] = useState('');
  const [strInfo, setStrInfo] = useState('');
  const [bolPending, startTransition] = useTransition();
  const [bolResending, startResendTransition] = useTransition();
  const { control, handleSubmit, setValue } = useForm<RegisterVerifyInput>({
    resolver: zodResolver(registerVerifySchema),
    defaultValues: { registerToken, otp: '' },
  });

  const handleVerify = (objInput: RegisterVerifyInput) =>
    startTransition(async () => {
      const objResult = await verifyRegisterAction(objInput);
      if (objResult.ok) return onVerified(objResult.data.otpauthUri);
      // Hết hạn / sai quá số lần / email vừa bị dùng → phải quay lại form đăng ký
      if (
        objResult.code === AUTH_ERROR_CODE.RegisterTokenInvalid ||
        objResult.code === AUTH_ERROR_CODE.EmailAlreadyExists
      )
        return onBack(objResult.message);
      setStrInfo('');
      setStrError(objResult.message);
      setValue('otp', '');
    });

  const handleResend = () =>
    startResendTransition(async () => {
      const objResult = await resendRegisterOtpAction(registerToken);
      if (objResult.ok) {
        setStrError('');
        setStrInfo('Đã gửi mã mới, vui lòng kiểm tra email.');
        setValue('otp', '');
        return setIntResendLeft(objResult.data.resendAfter);
      }
      if (objResult.code === AUTH_ERROR_CODE.RegisterTokenInvalid) return onBack(objResult.message);
      if (objResult.code === AUTH_ERROR_CODE.RegisterOtpCooldown && objResult.retryAfter)
        return setIntResendLeft(objResult.retryAfter);
      setStrError(describeAuthError(objResult));
    });

  useEffect(() => {
    if (intResendLeft <= 0) return;
    const intTimer = setTimeout(() => setIntResendLeft((x) => x - 1), 1000);
    return () => clearTimeout(intTimer);
  }, [intResendLeft]);

  return (
    <form onSubmit={handleSubmit(handleVerify)} className="space-y-4" noValidate>
      <p className="text-center text-sm text-slate-300">
        Mã xác thực 6 số đã được gửi tới <span className="font-semibold break-all text-amber-100">{email}</span>.
        <br />
        <span className="text-xs text-slate-400">Không thấy email? Kiểm tra thư mục Spam/Quảng cáo.</span>
      </p>
      <Controller
        control={control}
        name="otp"
        render={({ field, fieldState }) => (
          <OtpInput
            label="Mã xác thực email"
            value={field.value}
            onChange={field.onChange}
            onBlur={field.onBlur}
            error={fieldState.error?.message}
          />
        )}
      />
      {strInfo && <p className="text-xs text-emerald-300">{strInfo}</p>}
      <FormError message={strError} />
      <AuthSubmitButton pending={bolPending}>Xác nhận</AuthSubmitButton>
      <div className="flex items-center justify-between text-sm">
        <button type="button" onClick={() => onBack()} className="text-slate-400 hover:text-amber-100">
          Đổi email
        </button>
        <button
          type="button"
          onClick={handleResend}
          disabled={intResendLeft > 0 || bolResending}
          className="font-medium text-amber-200 hover:text-amber-100 disabled:cursor-not-allowed disabled:text-slate-500"
        >
          {bolResending ? 'Đang gửi…' : intResendLeft > 0 ? `Gửi lại mã (${intResendLeft}s)` : 'Gửi lại mã'}
        </button>
      </div>
    </form>
  );
}
