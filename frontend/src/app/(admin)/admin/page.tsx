import type { Metadata } from 'next';
import Link from 'next/link';

import { BarChart3, Lock, MonitorSmartphone, UserCheck, UserPlus, UserX, Users } from 'lucide-react';

import { getActiveSessions, getAdminUsers, getUserStats } from '@/features/admin/api';
import { LoadError } from '@/features/admin/components/load-error';
import { StatCard } from '@/features/admin/components/stat-card';
import { UserStatusBadge } from '@/features/admin/components/user-badges';
import { formatDateTime } from '@/lib/format';

export const metadata: Metadata = { title: 'Tổng quan' };

const RECENT_USER_COUNT = 5;

export default async function AdminOverviewPage() {
  const [objStats, lstSession, objRecent] = await Promise.all([
    getUserStats(),
    getActiveSessions(),
    getAdminUsers({ page: 1 }),
  ]);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Tổng quan</h1>
        <p className="mt-1 text-sm text-ink-muted">Tình hình người dùng và hệ thống</p>
      </div>

      {!objStats && <LoadError />}
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
        <StatCard label="Tổng người dùng" value={objStats?.totalUsers ?? null} icon={Users} />
        <StatCard label="Đang hoạt động" value={objStats?.activeUsers ?? null} icon={UserCheck} tone="good" />
        <StatCard label="Phiên đang đăng nhập" value={lstSession?.length ?? null} icon={MonitorSmartphone} />
        <StatCard label="Mới trong 7 ngày" value={objStats?.newUsers7Days ?? null} icon={UserPlus} tone="good" />
        <StatCard label="Bị vô hiệu hóa" value={objStats?.disabledUsers ?? null} icon={UserX} tone="warn" />
        <StatCard label="Khóa vĩnh viễn" value={objStats?.lockedUsers ?? null} icon={Lock} tone="bad" />
      </div>

      <div className="grid gap-6 xl:grid-cols-5">
        <section className="rounded-2xl bg-white ring-1 ring-line xl:col-span-3">
          <div className="flex items-center justify-between border-b border-line px-5 py-4">
            <h2 className="font-semibold">Người dùng mới nhất</h2>
            <Link href="/admin/users" className="text-sm font-medium text-ink-muted hover:text-ink">
              Xem tất cả
            </Link>
          </div>
          {objRecent ? (
            <ul className="divide-y divide-line">
              {objRecent.items.slice(0, RECENT_USER_COUNT).map((x) => (
                <li key={x.id} className="flex items-center gap-3 px-5 py-3">
                  <span className="flex size-9 shrink-0 items-center justify-center rounded-full bg-paper text-sm font-semibold uppercase">
                    {(x.fullName || x.email).slice(0, 1)}
                  </span>
                  <div className="min-w-0 flex-1">
                    <p className="truncate text-sm font-medium">{x.fullName || 'Chưa có tên'}</p>
                    <p className="truncate text-xs text-ink-muted">{x.email}</p>
                  </div>
                  <div className="hidden text-right sm:block">
                    <UserStatusBadge user={x} />
                    <p className="mt-1 text-xs text-ink-muted">{formatDateTime(x.createdDate)}</p>
                  </div>
                </li>
              ))}
              {objRecent.items.length === 0 && (
                <li className="px-5 py-6 text-sm text-ink-muted">Chưa có người dùng.</li>
              )}
            </ul>
          ) : (
            <div className="p-5">
              <LoadError />
            </div>
          )}
        </section>

        {/* Chỗ dành cho biểu đồ doanh số khi có API đơn hàng */}
        <section className="flex flex-col items-center justify-center gap-3 rounded-2xl border-2 border-dashed border-line p-8 text-center xl:col-span-2">
          <span className="flex size-12 items-center justify-center rounded-2xl bg-white text-ink-muted ring-1 ring-line">
            <BarChart3 aria-hidden className="size-6" />
          </span>
          <h2 className="font-semibold">Biểu đồ doanh số</h2>
          <p className="max-w-xs text-sm text-ink-muted">
            Tỉ lệ mua bán, doanh thu theo ngày sẽ hiển thị ở đây khi có dữ liệu đơn hàng.
          </p>
        </section>
      </div>
    </div>
  );
}
