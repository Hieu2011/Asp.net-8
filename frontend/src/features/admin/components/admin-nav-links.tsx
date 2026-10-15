'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';

import { BarChart3, LayoutDashboard, Package, ShoppingCart, Users } from 'lucide-react';
import type { LucideIcon } from 'lucide-react';

import { cn } from '@/lib/utils';

type NavItem = { href: string; label: string; icon: LucideIcon; soon?: boolean };

const NAV_ITEMS: NavItem[] = [
  { href: '/admin', label: 'Tổng quan', icon: LayoutDashboard },
  { href: '/admin/users', label: 'Người dùng', icon: Users },
  { href: '/admin/products', label: 'Sản phẩm', icon: Package, soon: true },
  { href: '/admin/orders', label: 'Đơn hàng', icon: ShoppingCart, soon: true },
  { href: '/admin/reports', label: 'Báo cáo', icon: BarChart3, soon: true },
];

type AdminNavLinksProps = { onNavigate?: () => void };

/** Menu trang quản trị — dùng chung cho sidebar desktop và Sheet trên mobile */
export function AdminNavLinks({ onNavigate }: AdminNavLinksProps) {
  const strPathname = usePathname();

  return (
    <ul className="space-y-1">
      {NAV_ITEMS.map((x) => {
        const bolActive = x.href === '/admin' ? strPathname === '/admin' : strPathname.startsWith(x.href);
        const strClass = 'flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm transition-colors';
        if (x.soon) {
          return (
            <li key={x.href}>
              <span aria-disabled className={cn(strClass, 'cursor-not-allowed text-slate-500')}>
                <x.icon aria-hidden className="size-4.5" />
                {x.label}
                <span className="ml-auto rounded-full bg-white/5 px-2 py-0.5 text-[10px] tracking-wide uppercase">
                  Sắp có
                </span>
              </span>
            </li>
          );
        }
        return (
          <li key={x.href}>
            <Link
              href={x.href}
              onClick={onNavigate}
              aria-current={bolActive ? 'page' : undefined}
              className={cn(
                strClass,
                bolActive ? 'bg-lamp/15 font-medium text-lamp' : 'text-slate-300 hover:bg-white/5 hover:text-white',
              )}
            >
              <x.icon aria-hidden className="size-4.5" />
              {x.label}
            </Link>
          </li>
        );
      })}
    </ul>
  );
}
