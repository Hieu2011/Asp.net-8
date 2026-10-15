import Link from 'next/link';

import { cn } from '@/lib/utils';

import { SORT_OPTIONS, buildProductsHref } from '../filter-url';
import type { ProductFilterState } from '../filter-url';

type SortBarProps = { state: ProductFilterState };

export function SortBar({ state }: SortBarProps) {
  return (
    <nav aria-label="Sắp xếp" className="flex flex-wrap gap-2">
      {SORT_OPTIONS.map((x) => (
        <Link
          key={x.value}
          href={buildProductsHref(state, { sort: x.value })}
          aria-current={state.sort === x.value ? 'true' : undefined}
          className={cn(
            'rounded-full border px-3 py-1.5 text-sm transition-colors',
            state.sort === x.value ? 'border-ink bg-ink text-white' : 'border-line bg-white hover:border-ink/40',
          )}
        >
          {x.label}
        </Link>
      ))}
    </nav>
  );
}
