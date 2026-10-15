import Link from 'next/link';

import { Store } from 'lucide-react';

import { getRoleName } from '@/lib/auth/roles';

import { AdminLogoutButton } from './admin-logout-button';
import { AdminMobileNav } from './admin-mobile-nav';

type AdminTopbarProps = { displayName: string; roleLevel: number };

export function AdminTopbar({ displayName, roleLevel }: AdminTopbarProps) {
  return (
    <header className="sticky top-0 z-30 flex h-16 items-center gap-2 border-b border-line bg-white/90 px-4 backdrop-blur lg:px-8">
      <AdminMobileNav />
      <Link
        href="/"
        className="inline-flex h-9 items-center gap-2 rounded-lg px-3 text-sm text-ink-muted hover:bg-paper hover:text-ink"
      >
        <Store aria-hidden className="size-4" />
        <span className="hidden sm:inline">Về cửa hàng</span>
      </Link>
      <div className="ml-auto flex min-w-0 items-center gap-3">
        <span className="flex size-9 shrink-0 items-center justify-center rounded-full bg-night text-sm font-bold text-lamp uppercase">
          {displayName.slice(0, 1)}
        </span>
        <div className="hidden min-w-0 leading-tight sm:block">
          <p className="max-w-48 truncate text-sm font-medium">{displayName}</p>
          <p className="text-xs text-ink-muted">{getRoleName(roleLevel)}</p>
        </div>
        <AdminLogoutButton />
      </div>
    </header>
  );
}
