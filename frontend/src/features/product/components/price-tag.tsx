import { calcDiscountPercent, formatPrice } from '@/lib/format';
import { cn } from '@/lib/utils';

type PriceTagProps = { price: number; oldPrice?: number; size?: 'sm' | 'lg' };

export function PriceTag({ price, oldPrice, size = 'sm' }: PriceTagProps) {
  const intDiscount = calcDiscountPercent(price, oldPrice);
  return (
    <div className="flex flex-wrap items-baseline gap-x-2 tabular-nums">
      <span className={cn('font-bold text-ink', size === 'lg' ? 'text-3xl' : 'text-base')}>{formatPrice(price)}</span>
      {intDiscount > 0 && (
        <>
          <span className={cn('text-ink-muted line-through', size === 'lg' ? 'text-base' : 'text-xs')}>
            {formatPrice(oldPrice!)}
          </span>
          <span className="rounded bg-sale/10 px-1 text-xs font-semibold text-sale">−{intDiscount}%</span>
        </>
      )}
    </div>
  );
}
