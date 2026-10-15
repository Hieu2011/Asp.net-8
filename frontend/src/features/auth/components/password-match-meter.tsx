import { Check } from 'lucide-react';

import { cn } from '@/lib/utils';

type PasswordMatchMeterProps = { password: string; confirm: string };

/** Thanh đầy dần theo số ký tự đã nhập lại so với mật khẩu — xanh khi phần đã gõ khớp, đỏ ngay khi lệch */
export function PasswordMatchMeter({ password, confirm }: PasswordMatchMeterProps) {
  if (!confirm) return null;
  const intLength = Math.max(password.length, 1);
  const bolMatched = confirm === password;
  const bolOnTrack = !bolMatched && password.startsWith(confirm);
  const intPercent = Math.min(confirm.length / intLength, 1) * 100;
  const strStatus = bolMatched ? 'Đã khớp' : bolOnTrack ? 'Đang khớp' : 'Chưa khớp';

  return (
    <div className="-mt-2 flex items-center gap-3">
      <div
        role="progressbar"
        aria-label="Mức khớp mật khẩu nhập lại"
        aria-valuemin={0}
        aria-valuemax={password.length}
        aria-valuenow={Math.min(confirm.length, password.length)}
        className="h-1 flex-1 overflow-hidden rounded-full bg-slate-700/70"
      >
        <div
          className={cn(
            'h-full rounded-full transition-[width,background-color] duration-300',
            bolMatched ? 'bg-emerald-400' : bolOnTrack ? 'bg-emerald-400/60' : 'bg-rose-400',
          )}
          style={{ width: `${intPercent}%` }}
        />
      </div>
      <span
        aria-live="polite"
        className={cn(
          'flex w-28 items-center justify-end gap-1 text-xs tabular-nums',
          bolMatched || bolOnTrack ? 'text-emerald-300' : 'text-rose-300',
        )}
      >
        {bolMatched && <Check aria-hidden className="size-3" strokeWidth={3} />}
        {strStatus} {confirm.length}/{password.length}
      </span>
    </div>
  );
}
