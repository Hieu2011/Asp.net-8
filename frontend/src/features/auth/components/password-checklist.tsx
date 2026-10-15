import { Check } from 'lucide-react';

import { cn } from '@/lib/utils';

import { PASSWORD_RULES } from '../schema';

type PasswordChecklistProps = { value: string };

const STRENGTH_LEVELS = [
  { label: 'Yếu', barClass: 'bg-rose-400', textClass: 'text-rose-300' },
  { label: 'Trung bình', barClass: 'bg-amber-400', textClass: 'text-amber-300' },
  { label: 'Khá', barClass: 'bg-lime-400', textClass: 'text-lime-300' },
  { label: 'Mạnh', barClass: 'bg-emerald-400', textClass: 'text-emerald-300' },
] as const;

// 0 = chưa nhập; đủ 4 điều kiện mới tới "Khá", thêm dài ≥ 12 + có chữ thường mới "Mạnh"
const getStrength = (strValue: string) => {
  if (!strValue) return 0;
  const intPassed = PASSWORD_RULES.filter((x) => x.test(strValue)).length;
  if (intPassed < PASSWORD_RULES.length) return intPassed <= 2 ? 1 : 2;
  return strValue.length >= 12 && /[a-z]/.test(strValue) ? 4 : 3;
};

/** Thanh độ mạnh + điều kiện mật khẩu tick xanh dần theo những gì người dùng gõ */
export function PasswordChecklist({ value }: PasswordChecklistProps) {
  const intStrength = getStrength(value);
  const objLevel = intStrength > 0 ? STRENGTH_LEVELS[intStrength - 1] : undefined;

  return (
    <div className="-mt-2 space-y-2.5">
      <div className="flex items-center gap-3">
        <div aria-hidden className="grid flex-1 grid-cols-4 gap-1.5">
          {STRENGTH_LEVELS.map((x, i) => (
            <span
              key={x.label}
              className={cn(
                'h-1 rounded-full transition-colors duration-300',
                i < intStrength && objLevel ? objLevel.barClass : 'bg-slate-700/70',
              )}
            />
          ))}
        </div>
        <span aria-live="polite" className={cn('w-20 text-right text-xs', objLevel?.textClass ?? 'text-slate-500')}>
          {objLevel ? objLevel.label : 'Độ mạnh'}
        </span>
      </div>
      <ul aria-label="Yêu cầu mật khẩu" className="grid grid-cols-2 gap-x-3 gap-y-1 text-xs">
        {PASSWORD_RULES.map((x) => {
          const bolPassed = x.test(value);
          return (
            <li
              key={x.label}
              className={cn(
                'flex items-center gap-1.5 transition-colors',
                bolPassed ? 'text-emerald-300' : 'text-slate-500',
              )}
            >
              <span
                aria-hidden
                className={cn(
                  'flex size-3.5 shrink-0 items-center justify-center rounded-full border transition-colors',
                  bolPassed ? 'border-emerald-400 bg-emerald-400/15' : 'border-slate-600',
                )}
              >
                {bolPassed && <Check className="size-2.5" strokeWidth={3} />}
              </span>
              {x.label}
              <span className="sr-only">{bolPassed ? '— đã đạt' : '— chưa đạt'}</span>
            </li>
          );
        })}
      </ul>
    </div>
  );
}
