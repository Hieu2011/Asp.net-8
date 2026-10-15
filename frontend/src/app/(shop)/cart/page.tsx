import type { Metadata } from 'next';

import { CartView } from '@/features/cart/components/cart-view';

export const metadata: Metadata = { title: 'Giỏ hàng' };

export default function CartPage() {
  return (
    <div className="mx-auto max-w-7xl px-4 py-8">
      <h1 className="mb-6 text-3xl font-bold tracking-tight">Giỏ hàng</h1>
      <CartView />
    </div>
  );
}
