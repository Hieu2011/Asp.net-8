import { cn } from '@/lib/utils';

import { CategoryIcon } from './category-icon';
import type { CategorySlug } from '../types';

type ProductVisualProps = {
  categorySlug: CategorySlug;
  tone: [string, string];
  size?: 'sm' | 'md' | 'lg';
  className?: string;
};

const ICON_SIZE = { sm: 'size-10', md: 'size-16', lg: 'size-32' } as const;

/** Ảnh minh họa tạm: nền theo màu sản phẩm + icon danh mục, có vệt sáng đèn phía trên */
export function ProductVisual({ categorySlug, tone, size = 'md', className }: ProductVisualProps) {
  const [strFrom, strTo] = tone;
  return (
    <div
      className={cn('relative flex items-center justify-center overflow-hidden', className)}
      style={{ background: `radial-gradient(120% 90% at 50% 0%, ${strFrom} 0%, ${strTo} 100%)` }}
    >
      <div aria-hidden className="absolute inset-x-[15%] top-0 h-1/2 bg-white/25 blur-2xl" />
      <CategoryIcon
        slug={categorySlug}
        strokeWidth={1.25}
        className={cn('relative text-white/85 drop-shadow', ICON_SIZE[size])}
      />
    </div>
  );
}
