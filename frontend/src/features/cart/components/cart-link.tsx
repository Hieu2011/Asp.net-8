'use client';

import Link from 'next/link';

import { ShoppingBag } from 'lucide-react';

import { useHydrated } from '@/hooks/use-hydrated';

import { selectCartCount, useCartStore } from '../store';

export function CartLink() {
  const bolHydrated = useHydrated();
  const intCount = useCartStore(selectCartCount);
  const intShown = bolHydrated ? intCount : 0;
  return (
    <Link
      href="/cart"
      aria-label={`Giỏ hàng, ${intShown} sản phẩm`}
      className="relative inline-flex size-10 items-center justify-center rounded-full text-slate-100 hover:bg-white/10 focus-visible:ring-2 focus-visible:ring-lamp focus-visible:outline-none"
    >
      <ShoppingBag className="size-5" />
      {intShown > 0 && (
        <span className="absolute -top-0.5 -right-0.5 min-w-5 rounded-full bg-lamp px-1 text-center text-[11px] leading-5 font-bold text-night tabular-nums">
          {intShown > 99 ? '99+' : intShown}
        </span>
      )}
    </Link>
  );
}
