import type { Metadata } from 'next';
import Link from 'next/link';

import { getCategories, getCategory, getProducts, isCategorySlug } from '@/features/product/api';
import { ProductFilters } from '@/features/product/components/product-filters';
import { ProductGrid } from '@/features/product/components/product-grid';
import { SortBar } from '@/features/product/components/sort-bar';
import { buildProductsHref, isProductSort } from '@/features/product/filter-url';
import type { ProductFilterState } from '@/features/product/filter-url';
import { cn } from '@/lib/utils';

export const metadata: Metadata = { title: 'Sản phẩm' };

const firstValue = (objValue: string | string[] | undefined) => (Array.isArray(objValue) ? objValue[0] : objValue);

export default async function ProductsPage({ searchParams }: PageProps<'/products'>) {
  const dicParam = await searchParams;
  const strCategory = firstValue(dicParam.category);
  const strSort = firstValue(dicParam.sort);
  const intMaxPrice = Number(firstValue(dicParam.maxPrice)) || undefined;
  const objState: ProductFilterState = {
    category: isCategorySlug(strCategory) ? strCategory : undefined,
    keyword: firstValue(dicParam.q)?.trim() || undefined,
    sort: isProductSort(strSort) ? strSort : 'popular',
    maxPrice: intMaxPrice,
  };
  const [lstCategory, objCategory, objPage] = await Promise.all([
    getCategories(),
    objState.category ? getCategory(objState.category) : undefined,
    getProducts({ ...objState, page: Number(firstValue(dicParam.page)) || 1 }),
  ]);
  const strTitle = objState.keyword ? `Kết quả cho “${objState.keyword}”` : (objCategory?.name ?? 'Tất cả sản phẩm');

  return (
    <div className="mx-auto max-w-7xl px-4 py-8">
      <div className="mb-6 flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">{strTitle}</h1>
          <p className="mt-1 text-sm text-ink-muted">{objPage.total} sản phẩm</p>
        </div>
        <SortBar state={objState} />
      </div>

      <div className="grid gap-8 lg:grid-cols-[220px_1fr]">
        <aside className="lg:sticky lg:top-32 lg:h-fit">
          <ProductFilters categories={lstCategory} state={objState} />
        </aside>
        <div>
          {objPage.items.length > 0 ? (
            <ProductGrid products={objPage.items} />
          ) : (
            <div className="rounded-2xl bg-white px-6 py-16 text-center ring-1 ring-line">
              <p className="font-medium">Không tìm thấy sản phẩm phù hợp</p>
              <Link href="/products" className="mt-2 inline-block text-sm text-ink-muted underline underline-offset-4">
                Xóa bộ lọc
              </Link>
            </div>
          )}
          {objPage.pageCount > 1 && (
            <nav aria-label="Phân trang" className="mt-8 flex justify-center gap-2">
              {Array.from({ length: objPage.pageCount }, (_, i) => i + 1).map((intPage) => (
                <Link
                  key={intPage}
                  href={buildProductsHref(objState, { page: intPage })}
                  aria-current={intPage === objPage.page ? 'page' : undefined}
                  className={cn(
                    'flex size-10 items-center justify-center rounded-full text-sm tabular-nums',
                    intPage === objPage.page
                      ? 'bg-night text-lamp-soft'
                      : 'bg-white ring-1 ring-line hover:ring-ink/40',
                  )}
                >
                  {intPage}
                </Link>
              ))}
            </nav>
          )}
        </div>
      </div>
    </div>
  );
}
