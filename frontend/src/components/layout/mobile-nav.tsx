'use client';

import Link from 'next/link';

import { Menu } from 'lucide-react';

import { Sheet, SheetClose, SheetContent, SheetHeader, SheetTitle, SheetTrigger } from '@/components/ui/sheet';
import { CategoryIcon } from '@/features/product/components/category-icon';
import type { Category } from '@/features/product/types';

type MobileNavProps = { categories: Category[] };

export function MobileNav({ categories }: MobileNavProps) {
  return (
    <Sheet>
      <SheetTrigger
        aria-label="Mở menu"
        className="inline-flex size-10 items-center justify-center rounded-full text-slate-100 hover:bg-white/10 lg:hidden"
      >
        <Menu className="size-5" />
      </SheetTrigger>
      <SheetContent side="left" className="w-80">
        <SheetHeader>
          <SheetTitle>Danh mục</SheetTitle>
        </SheetHeader>
        <nav aria-label="Danh mục sản phẩm" className="px-2">
          <ul className="space-y-1">
            {categories.map((x) => (
              <li key={x.slug}>
                <SheetClose
                  render={<Link href={`/products?category=${x.slug}`} />}
                  nativeButton={false}
                  className="flex w-full items-center gap-3 rounded-lg px-3 py-2.5 text-sm hover:bg-muted"
                >
                  <CategoryIcon slug={x.slug} className="size-5 text-ink-muted" />
                  {x.name}
                </SheetClose>
              </li>
            ))}
          </ul>
        </nav>
      </SheetContent>
    </Sheet>
  );
}
