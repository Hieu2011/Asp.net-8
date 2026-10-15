import { ProductCard } from './product-card';
import type { Product } from '../types';

type ProductGridProps = { products: Product[] };

export function ProductGrid({ products }: ProductGridProps) {
  return (
    <ul className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
      {products.map((x) => (
        <li key={x.id} className="flex">
          <ProductCard product={x} />
        </li>
      ))}
    </ul>
  );
}
