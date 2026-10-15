import type { LucideIcon } from 'lucide-react';

import { formatNumber } from '@/lib/format';
import { cn } from '@/lib/utils';

type StatCardProps = {
  label: string;
  value: number | null;
  icon: LucideIcon;
  tone?: 'default' | 'good' | 'warn' | 'bad';
};

const TONE_CLASS: Record<NonNullable<StatCardProps['tone']>, string> = {
  default: 'bg-night text-lamp',
  good: 'bg-emerald-50 text-emerald-600',
  warn: 'bg-amber-50 text-amber-600',
  bad: 'bg-rose-50 text-rose-600',
};

export function StatCard({ label, value, icon, tone = 'default' }: StatCardProps) {
  const Icon = icon;
  return (
    <div className="flex items-center gap-4 rounded-2xl bg-white p-5 ring-1 ring-line">
      <span className={cn('flex size-11 shrink-0 items-center justify-center rounded-xl', TONE_CLASS[tone])}>
        <Icon aria-hidden className="size-5" />
      </span>
      <div className="min-w-0">
        <p className="truncate text-sm text-ink-muted">{label}</p>
        <p className="text-2xl font-bold tracking-tight tabular-nums">{value === null ? '—' : formatNumber(value)}</p>
      </div>
    </div>
  );
}
