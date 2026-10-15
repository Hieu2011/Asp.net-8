import type { Metadata } from 'next';
import Link from 'next/link';
import { notFound } from 'next/navigation';

import { RotateCcw, ShieldCheck, Truck } from 'lucide-react';

import { AddToCart } from '@/features/cart/components/add-to-cart';
import { getCategory, getProductBySlug, getRelatedProducts } from '@/features/product/api';
import { PriceTag } from '@/features/product/components/price-tag';
import { ProductGrid } from '@/features/product/components/product-grid';
import { ProductVisual } from '@/features/product/components/product-visual';
import { RatingLine } from '@/features/product/components/rating-line';

export async function generateMetadata({ params }: PageProps<'/products/[slug]'>): Promise<Metadata> {
  const objProduct = await getProductBySlug((await params).slug);
  return objProduct ? { title: objProduct.name, description: objProduct.summary } : { title: 'Không tìm thấy' };
}

const SERVICES = [
  { icon: Truck, text: 'Giao nhanh 2 giờ nội thành, miễn phí đơn từ 500.000đ' },
  { icon: ShieldCheck, text: 'Bảo hành chính hãng 12 tháng' },
  { icon: RotateCcw, text: 'Đổi trả miễn phí trong 30 ngày' },
];

export default async function ProductDetailPage({ params }: PageProps<'/products/[slug]'>) {
  const objProduct = await getProductBySlug((await params).slug);
  if (!objProduct) notFound();
  const [objCategory, lstRelated] = await Promise.all([
    getCategory(objProduct.categorySlug),
    getRelatedProducts(objProduct),
  ]);

  return (
    <div className="mx-auto max-w-7xl px-4 py-8">
      <nav aria-label="Đường dẫn" className="mb-6 text-sm text-ink-muted">
        <ol className="flex flex-wrap gap-2">
          <li>
            <Link href="/products" className="hover:text-ink">
              Sản phẩm
            </Link>
          </li>
          <li aria-hidden>/</li>
          <li>
            <Link href={`/products?category=${objProduct.categorySlug}`} className="hover:text-ink">
              {objCategory?.name}
            </Link>
          </li>
        </ol>
      </nav>

      <div className="grid gap-10 lg:grid-cols-2">
        <ProductVisual
          categorySlug={objProduct.categorySlug}
          tone={objProduct.tone}
          size="lg"
          className="aspect-square rounded-3xl"
        />
        <div className="space-y-6">
          <div className="space-y-2">
            <p className="text-sm text-ink-muted">{objProduct.brand}</p>
            <h1 className="text-3xl font-bold tracking-tight text-balance">{objProduct.name}</h1>
            <RatingLine
              rating={objProduct.rating}
              reviewCount={objProduct.reviewCount}
              soldCount={objProduct.soldCount}
            />
          </div>
          <PriceTag price={objProduct.price} oldPrice={objProduct.oldPrice} size="lg" />
          <p className="text-ink-muted">{objProduct.summary}</p>
          <AddToCart product={objProduct} />
          <ul className="space-y-2 rounded-2xl bg-white p-4 text-sm ring-1 ring-line">
            {SERVICES.map((x) => (
              <li key={x.text} className="flex items-center gap-3">
                <x.icon aria-hidden className="size-4 shrink-0 text-ink-muted" />
                {x.text}
              </li>
            ))}
          </ul>
        </div>
      </div>

      <section aria-labelledby="spec-heading" className="mt-14 max-w-3xl">
        <h2 id="spec-heading" className="text-2xl font-bold tracking-tight">
          Thông số kỹ thuật
        </h2>
        <dl className="mt-4 divide-y divide-line rounded-2xl bg-white ring-1 ring-line">
          {objProduct.specs.map((x) => (
            <div key={x.label} className="grid grid-cols-[140px_1fr] gap-4 px-4 py-3 text-sm sm:grid-cols-[200px_1fr]">
              <dt className="text-ink-muted">{x.label}</dt>
              <dd className="font-medium">{x.value}</dd>
            </div>
          ))}
        </dl>
      </section>

      <section aria-labelledby="related-heading" className="mt-14">
        <h2 id="related-heading" className="text-2xl font-bold tracking-tight">
          Có thể bạn cũng thích
        </h2>
        <div className="mt-5">
          <ProductGrid products={lstRelated} />
        </div>
      </section>
    </div>
  );
}
