'use client';

import { useId, useState } from 'react';
import type { ComponentProps } from 'react';

import { Eye, EyeOff } from 'lucide-react';

import { cn } from '@/lib/utils';

type AuthFieldProps = ComponentProps<'input'> & { label: string; error?: string };

/** Input gạch chân + label nổi (floating label); ô mật khẩu có nút ẩn/hiện */
export function AuthField({ label, error, className, type, ...props }: AuthFieldProps) {
  const strID = useId();
  const [bolShowPassword, setBolShowPassword] = useState(false);
  const strErrorID = `${strID}-error`;
  const bolIsPassword = type === 'password';

  return (
    <div className="relative pt-4">
      <input
        id={strID}
        type={bolIsPassword && bolShowPassword ? 'text' : type}
        placeholder=" "
        aria-invalid={error ? true : undefined}
        aria-describedby={error ? strErrorID : undefined}
        className={cn(
          'peer w-full border-0 border-b border-slate-600 bg-transparent px-0 py-2 text-base text-slate-50 transition-colors outline-none placeholder-shown:border-slate-600 focus:border-amber-200 aria-invalid:border-rose-400',
          bolIsPassword && 'pr-10',
          className,
        )}
        {...props}
      />
      <label
        htmlFor={strID}
        className="pointer-events-none absolute top-0 left-0 text-xs text-slate-400 transition-all peer-placeholder-shown:top-6 peer-placeholder-shown:text-base peer-focus:top-0 peer-focus:text-xs peer-focus:text-amber-200"
      >
        {label}
      </label>
      {bolIsPassword && (
        <button
          type="button"
          aria-label={bolShowPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'}
          aria-pressed={bolShowPassword}
          aria-controls={strID}
          onClick={() => setBolShowPassword((x) => !x)}
          className="absolute top-4.5 right-0 flex size-9 items-center justify-center rounded-full text-slate-400 transition-colors hover:text-amber-100 focus-visible:ring-2 focus-visible:ring-amber-200 focus-visible:outline-none"
        >
          {bolShowPassword ? <EyeOff aria-hidden className="size-4.5" /> : <Eye aria-hidden className="size-4.5" />}
        </button>
      )}
      {error && (
        <p id={strErrorID} className="mt-1 text-xs text-rose-300">
          {error}
        </p>
      )}
    </div>
  );
}
