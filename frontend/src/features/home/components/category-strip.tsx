import Link from 'next/link';

import { CategoryIcon } from '@/features/product/components/category-icon';
import type { Category } from '@/features/product/types';

type CategoryStripProps = { categories: Category[] };

export function CategoryStrip({ categories }: CategoryStripProps) {
  return (
    <section aria-labelledby="category-heading" className="mx-auto max-w-7xl px-4 pt-14">
      <h2 id="category-heading" className="text-2xl font-bold tracking-tight">
        Mua theo nhu cầu
      </h2>
      <ul className="mt-5 grid grid-cols-2 gap-px overflow-hidden rounded-2xl bg-line ring-1 ring-line sm:grid-cols-4">
        {categories.map((x) => (
          <li key={x.slug}>
            <Link
              href={`/products?category=${x.slug}`}
              className="group flex h-full items-center gap-4 bg-white p-4 transition-colors hover:bg-paper focus-visible:bg-paper focus-visible:outline-none"
            >
              <span className="flex size-12 shrink-0 items-center justify-center rounded-xl bg-paper text-ink transition-colors group-hover:bg-night group-hover:text-lamp">
                <CategoryIcon slug={x.slug} className="size-6" strokeWidth={1.5} />
              </span>
              <span className="min-w-0">
                <span className="block font-semibold text-ink">{x.name}</span>
                <span className="block truncate text-sm text-ink-muted">{x.tagline}</span>
              </span>
            </Link>
          </li>
        ))}
      </ul>
    </section>
  );
}
