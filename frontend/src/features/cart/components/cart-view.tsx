'use client';

import Link from 'next/link';

import { Trash2 } from 'lucide-react';

import { Button } from '@/components/ui/button';
import { ProductVisual } from '@/features/product/components/product-visual';
import { useHydrated } from '@/hooks/use-hydrated';
import { formatPrice } from '@/lib/format';

import { selectCartTotal, useCartStore } from '../store';
import { QuantityStepper } from './quantity-stepper';

const FREE_SHIP_THRESHOLD = 500_000;
const SHIPPING_FEE = 30_000;

export function CartView() {
  const bolHydrated = useHydrated();
  const lstItem = useCartStore((x) => x.lstItem);
  const decSubtotal = useCartStore(selectCartTotal);
  const setQuantity = useCartStore((x) => x.setQuantity);
  const removeItem = useCartStore((x) => x.removeItem);

  if (!bolHydrated) return <div className="h-64 animate-pulse rounded-2xl bg-white ring-1 ring-line" />;

  if (lstItem.length === 0) {
    return (
      <div className="rounded-2xl bg-white px-6 py-16 text-center ring-1 ring-line">
        <p className="text-lg font-medium text-ink">Giỏ hàng đang trống</p>
        <p className="mt-1 text-sm text-ink-muted">Dạo một vòng, biết đâu góc làm việc của bạn đang thiếu gì đó.</p>
        <Button render={<Link href="/products" />} nativeButton={false} className="mt-6 h-10 rounded-full px-6">
          Xem sản phẩm
        </Button>
      </div>
    );
  }

  const decShipping = decSubtotal >= FREE_SHIP_THRESHOLD ? 0 : SHIPPING_FEE;
  return (
    <div className="grid gap-6 lg:grid-cols-[1fr_340px]">
      <ul className="divide-y divide-line rounded-2xl bg-white ring-1 ring-line">
        {lstItem.map((x) => (
          <li key={x.key} className="flex gap-4 p-4">
            <ProductVisual
              categorySlug={x.categorySlug}
              tone={x.tone}
              size="sm"
              className="size-20 shrink-0 rounded-xl"
            />
            <div className="flex min-w-0 flex-1 flex-col gap-1">
              <Link href={`/products/${x.slug}`} className="line-clamp-2 text-sm font-medium text-ink hover:underline">
                {x.name}
              </Link>
              <p className="text-xs text-ink-muted">Màu: {x.color}</p>
              <div className="mt-auto flex items-center justify-between gap-2">
                <QuantityStepper size="sm" value={x.quantity} onChange={(intValue) => setQuantity(x.key, intValue)} />
                <span className="font-semibold text-ink tabular-nums">{formatPrice(x.price * x.quantity)}</span>
              </div>
            </div>
            <button
              type="button"
              aria-label={`Xóa ${x.name}`}
              onClick={() => removeItem(x.key)}
              className="self-start rounded-full p-1.5 text-ink-muted hover:bg-paper hover:text-sale"
            >
              <Trash2 className="size-4" />
            </button>
          </li>
        ))}
      </ul>

      <aside className="h-fit space-y-3 rounded-2xl bg-white p-5 ring-1 ring-line lg:sticky lg:top-24">
        <h2 className="text-lg font-semibold text-ink">Tóm tắt đơn hàng</h2>
        <dl className="space-y-2 text-sm tabular-nums">
          <div className="flex justify-between">
            <dt className="text-ink-muted">Tạm tính</dt>
            <dd>{formatPrice(decSubtotal)}</dd>
          </div>
          <div className="flex justify-between">
            <dt className="text-ink-muted">Phí giao hàng</dt>
            <dd>{decShipping === 0 ? 'Miễn phí' : formatPrice(decShipping)}</dd>
          </div>
          <div className="flex justify-between border-t border-line pt-3 text-base font-semibold">
            <dt>Tổng cộng</dt>
            <dd>{formatPrice(decSubtotal + decShipping)}</dd>
          </div>
        </dl>
        {decShipping > 0 && (
          <p className="text-xs text-ink-muted">
            Mua thêm {formatPrice(FREE_SHIP_THRESHOLD - decSubtotal)} để được miễn phí giao hàng.
          </p>
        )}
        <Button
          render={<Link href="/checkout" />}
          nativeButton={false}
          className="h-11 w-full rounded-full bg-night text-base font-semibold text-lamp-soft hover:bg-night-soft"
        >
          Thanh toán
        </Button>
      </aside>
    </div>
  );
}
