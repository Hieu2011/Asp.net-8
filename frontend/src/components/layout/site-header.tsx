import Link from 'next/link';

import { Search } from 'lucide-react';

import { CartLink } from '@/features/cart/components/cart-link';
import { getCategories } from '@/features/product/api';
import { CategoryIcon } from '@/features/product/components/category-icon';
import { canViewAdmin } from '@/lib/auth/roles';
import { getCurrentUser } from '@/lib/auth/session';

import { Logo } from './logo';
import { MobileNav } from './mobile-nav';
import { UserMenu } from './user-menu';

export async function SiteHeader() {
  const [lstCategory, objUser] = await Promise.all([getCategories(), getCurrentUser()]);
  return (
    <header className="sticky top-0 z-40 bg-night text-slate-100">
      <div className="mx-auto flex h-16 max-w-7xl items-center gap-3 px-4">
        <MobileNav categories={lstCategory} />
        <Logo />
        {/* Tìm kiếm = form GET → trang danh sách, chạy được cả khi chưa tải JS */}
        <form action="/products" role="search" className="mx-auto hidden w-full max-w-xl md:block">
          <label className="relative block">
            <span className="sr-only">Tìm sản phẩm</span>
            <Search aria-hidden className="absolute top-1/2 left-4 size-4 -translate-y-1/2 text-slate-400" />
            <input
              name="q"
              type="search"
              placeholder="Tìm bàn phím, tai nghe, củ sạc…"
              className="h-10 w-full rounded-full border border-white/10 bg-night-soft pr-4 pl-10 text-sm text-slate-100 placeholder:text-slate-400 focus:border-lamp/60 focus:outline-none"
            />
          </label>
        </form>
        <div className="ml-auto flex items-center gap-1 md:ml-0">
          <UserMenu
            displayName={objUser ? objUser.fullName || objUser.email : null}
            showAdminLink={!!objUser && canViewAdmin(objUser.roleLevel)}
          />
          <CartLink />
        </div>
      </div>
      <nav aria-label="Danh mục sản phẩm" className="hidden border-t border-white/5 lg:block">
        <ul className="mx-auto flex max-w-7xl items-center gap-1 px-4">
          {lstCategory.map((x) => (
            <li key={x.slug}>
              <Link
                href={`/products?category=${x.slug}`}
                className="flex items-center gap-2 rounded-md px-3 py-2.5 text-sm text-slate-300 hover:bg-white/5 hover:text-lamp-soft"
              >
                <CategoryIcon slug={x.slug} className="size-4" />
                {x.name}
              </Link>
            </li>
          ))}
        </ul>
      </nav>
    </header>
  );
}
