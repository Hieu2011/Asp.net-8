import Link from 'next/link';
import type { ReactNode } from 'react';

import { Button } from '@/components/ui/button';

type PagePlaceholderProps = { title: string; children: ReactNode };

/** Trang chức năng chưa làm — giữ link không bị 404 */
export function PagePlaceholder({ title, children }: PagePlaceholderProps) {
  return (
    <div className="mx-auto max-w-2xl px-4 py-16 text-center">
      <h1 className="text-3xl font-bold tracking-tight">{title}</h1>
      <div className="mt-3 text-ink-muted">{children}</div>
      <Button render={<Link href="/products" />} nativeButton={false} className="mt-8 h-10 rounded-full px-6">
        Tiếp tục mua sắm
      </Button>
    </div>
  );
}
