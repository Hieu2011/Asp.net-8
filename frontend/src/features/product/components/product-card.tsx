import Link from 'next/link';

import { PriceTag } from './price-tag';
import { ProductVisual } from './product-visual';
import { RatingLine } from './rating-line';
import type { Product } from '../types';

type ProductCardProps = { product: Product };

export function ProductCard({ product }: ProductCardProps) {
  return (
    <Link
      href={`/products/${product.slug}`}
      className="group flex w-full flex-col rounded-2xl bg-white p-2 ring-1 ring-line transition-colors hover:ring-ink/30 focus-visible:ring-2 focus-visible:ring-lamp focus-visible:outline-none"
    >
      <div className="relative">
        <ProductVisual categorySlug={product.categorySlug} tone={product.tone} className="aspect-square rounded-xl" />
        {product.badge && (
          <span className="absolute top-2 left-2 rounded-full bg-night px-2 py-0.5 text-xs font-medium text-lamp-soft">
            {product.badge}
          </span>
        )}
      </div>
      <div className="flex flex-1 flex-col gap-1.5 px-1.5 pt-3 pb-1.5">
        <p className="text-xs text-ink-muted">{product.brand}</p>
        <h3 className="line-clamp-2 text-sm leading-snug font-medium text-ink group-hover:underline">{product.name}</h3>
        <div className="mt-auto space-y-1 pt-1">
          <PriceTag price={product.price} oldPrice={product.oldPrice} />
          <RatingLine rating={product.rating} reviewCount={product.reviewCount} soldCount={product.soldCount} />
        </div>
      </div>
    </Link>
  );
}
