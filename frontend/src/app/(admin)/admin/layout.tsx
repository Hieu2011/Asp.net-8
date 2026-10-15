import type { Metadata } from 'next';
import { notFound, redirect } from 'next/navigation';

import { Logo } from '@/components/layout/logo';
import { AdminNavLinks } from '@/features/admin/components/admin-nav-links';
import { AdminTopbar } from '@/features/admin/components/admin-topbar';
import { canViewAdmin } from '@/lib/auth/roles';
import { getCurrentUser } from '@/lib/auth/session';

export const metadata: Metadata = { title: { template: '%s · Quản trị HPM', default: 'Quản trị HPM' } };

export default async function AdminLayout({ children }: LayoutProps<'/admin'>) {
  // Proxy chỉ check cookie — kiểm tra quyền thật với AuthService; không đủ quyền thì giả như trang không tồn tại
  const objUser = await getCurrentUser();
  if (!objUser) redirect('/login?next=/admin');
  if (!canViewAdmin(objUser.roleLevel)) notFound();

  return (
    <div className="flex min-h-dvh bg-paper text-ink">
      <aside className="sticky top-0 hidden h-dvh w-64 shrink-0 flex-col bg-night text-slate-100 lg:flex">
        <div className="flex h-16 items-center px-6">
          <Logo />
        </div>
        <nav aria-label="Menu quản trị" className="flex-1 overflow-y-auto px-3 py-4">
          <AdminNavLinks />
        </nav>
      </aside>
      <div className="flex min-w-0 flex-1 flex-col">
        <AdminTopbar displayName={objUser.fullName || objUser.email} roleLevel={objUser.roleLevel} />
        <main className="flex-1 px-4 py-6 lg:px-8">{children}</main>
      </div>
    </div>
  );
}
