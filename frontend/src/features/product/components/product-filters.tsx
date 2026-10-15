import Link from 'next/link';
import type { ReactNode } from 'react';

import { cn } from '@/lib/utils';

import { PRICE_OPTIONS, buildProductsHref } from '../filter-url';
import type { ProductFilterState } from '../filter-url';
import type { Category } from '../types';

type FilterLinkProps = { href: string; active: boolean; children: ReactNode };

function FilterLink({ href, active, children }: FilterLinkProps) {
  return (
    <Link
      href={href}
      aria-current={active ? 'true' : undefined}
      className={cn(
        'block rounded-lg px-3 py-2 text-sm transition-colors',
        active ? 'bg-night font-medium text-lamp-soft' : 'text-ink hover:bg-white',
      )}
    >
      {children}
    </Link>
  );
}

type ProductFiltersProps = { categories: Category[]; state: ProductFilterState };

// Bộ lọc bằng link → server render, không cần JS, chia sẻ URL được
export function ProductFilters({ categories, state }: ProductFiltersProps) {
  return (
    <div className="space-y-6">
      <section>
        <h2 className="mb-2 px-3 text-xs font-semibold text-ink-muted">Danh mục</h2>
        <FilterLink href={buildProductsHref(state, { category: undefined })} active={!state.category}>
          Tất cả
        </FilterLink>
        {categories.map((x) => (
          <FilterLink
            key={x.slug}
            href={buildProductsHref(state, { category: x.slug })}
            active={state.category === x.slug}
          >
            {x.name}
          </FilterLink>
        ))}
      </section>
      <section>
        <h2 className="mb-2 px-3 text-xs font-semibold text-ink-muted">Khoảng giá</h2>
        <FilterLink href={buildProductsHref(state, { maxPrice: undefined })} active={!state.maxPrice}>
          Mọi mức giá
        </FilterLink>
        {PRICE_OPTIONS.map((x) => (
          <FilterLink
            key={x.value}
            href={buildProductsHref(state, { maxPrice: x.value })}
            active={state.maxPrice === x.value}
          >
            {x.label}
          </FilterLink>
        ))}
      </section>
    </div>
  );
}
