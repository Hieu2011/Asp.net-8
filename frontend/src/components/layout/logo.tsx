import Link from 'next/link';

import { cn } from '@/lib/utils';

type LogoProps = { className?: string };

/** Wordmark HPM + chấm sáng như bóng đèn — nối với trang đăng nhập */
export function Logo({ className }: LogoProps) {
  return (
    <Link href="/" aria-label="HPM Shop — trang chủ" className={cn('inline-flex items-center gap-2', className)}>
      <span aria-hidden className="relative flex size-7 items-center justify-center rounded-full bg-night-soft">
        <span className="size-2.5 rounded-full bg-lamp shadow-[0_0_12px_3px_rgba(245,196,81,0.6)]" />
      </span>
      <span className="text-lg font-bold tracking-tight">
        HPM<span className="font-medium text-lamp"> Shop</span>
      </span>
    </Link>
  );
}
