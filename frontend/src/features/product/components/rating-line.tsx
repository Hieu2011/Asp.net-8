import { Star } from 'lucide-react';

import { formatNumber } from '@/lib/format';

type RatingLineProps = { rating: number; reviewCount: number; soldCount: number };

export function RatingLine({ rating, reviewCount, soldCount }: RatingLineProps) {
  return (
    <p className="flex items-center gap-1 text-xs text-ink-muted">
      <Star aria-hidden className="size-3.5 fill-lamp text-lamp" />
      <span className="font-medium text-ink">{rating.toFixed(1)}</span>
      <span>({formatNumber(reviewCount)})</span>
      <span aria-hidden className="mx-1 h-3 w-px bg-line" />
      <span>Đã bán {formatNumber(soldCount)}</span>
    </p>
  );
}
