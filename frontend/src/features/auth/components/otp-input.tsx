'use client';

import { useId, useState } from 'react';

import { cn } from '@/lib/utils';

const OTP_LENGTH = 6;

type OtpInputProps = {
  value: string;
  onChange: (strValue: string) => void;
  onBlur?: () => void;
  label: string;
  error?: string;
};

/** 6 ô nhập OTP: chỉ nhận số, dán/tự điền SMS được, thanh sáng trượt theo ô đang nhập */
export function OtpInput({ value, onChange, onBlur, label, error }: OtpInputProps) {
  const strID = useId();
  const [bolFocused, setBolFocused] = useState(false);
  // 1 input thật phủ lên 6 ô → giữ nguyên hành vi gõ/xóa/dán/autofill của trình duyệt
  const intActiveIndex = Math.min(value.length, OTP_LENGTH - 1);

  return (
    <div className="space-y-2">
      <label htmlFor={strID} className="text-xs text-slate-400">
        {label}
      </label>
      <div className="relative">
        <div aria-hidden className="grid grid-cols-6 gap-2">
          {Array.from({ length: OTP_LENGTH }, (_, i) => (
            <div
              key={i}
              className={cn(
                'flex h-11 items-center justify-center rounded-lg border bg-white/[0.03] font-mono text-xl text-slate-50 transition-colors',
                error ? 'border-rose-400/70' : 'border-slate-700',
                bolFocused && i === intActiveIndex && !error && 'border-amber-200/70 bg-amber-100/[0.06]',
              )}
            >
              {value[i] ?? ''}
            </div>
          ))}
        </div>
        {/* Thanh sáng trượt tới ô kế tiếp cần nhập */}
        <div
          aria-hidden
          className={cn(
            'absolute -bottom-2 left-0 h-0.5 rounded-full bg-amber-200 shadow-[0_0_10px_rgba(253,224,140,0.8)] transition-all duration-300 ease-out motion-reduce:transition-none',
            bolFocused ? 'opacity-100' : 'opacity-0',
          )}
          style={{
            width: 'calc((100% - 2.5rem) / 6)',
            transform: `translateX(calc(${intActiveIndex} * (100% + 0.5rem)))`,
          }}
        />
        <input
          id={strID}
          value={value}
          inputMode="numeric"
          autoComplete="one-time-code"
          pattern="\d*"
          maxLength={OTP_LENGTH}
          aria-invalid={error ? true : undefined}
          aria-describedby={error ? `${strID}-error` : undefined}
          onChange={(objEvent) => onChange(objEvent.target.value.replace(/\D/g, '').slice(0, OTP_LENGTH))}
          onFocus={() => setBolFocused(true)}
          onBlur={() => {
            setBolFocused(false);
            onBlur?.();
          }}
          className="absolute inset-0 h-full w-full cursor-text opacity-0"
        />
      </div>
      {error && (
        <p id={`${strID}-error`} className="pt-1 text-xs text-rose-300">
          {error}
        </p>
      )}
    </div>
  );
}
