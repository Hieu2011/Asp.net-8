import { Minus, Plus } from 'lucide-react';

import { cn } from '@/lib/utils';

type QuantityStepperProps = {
  value: number;
  onChange: (intValue: number) => void;
  size?: 'sm' | 'md';
};

export function QuantityStepper({ value, onChange, size = 'md' }: QuantityStepperProps) {
  const strButtonClass = cn(
    'flex items-center justify-center text-ink hover:text-ink-muted',
    size === 'sm' ? 'size-8' : 'size-10',
  );
  return (
    <div className="flex items-center rounded-full border border-line bg-white">
      <button type="button" aria-label="Giảm số lượng" onClick={() => onChange(value - 1)} className={strButtonClass}>
        <Minus className="size-3.5" />
      </button>
      <span aria-live="polite" className="w-7 text-center text-sm font-medium tabular-nums">
        {value}
      </span>
      <button type="button" aria-label="Tăng số lượng" onClick={() => onChange(value + 1)} className={strButtonClass}>
        <Plus className="size-3.5" />
      </button>
    </div>
  );
}
