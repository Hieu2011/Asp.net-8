import type { Metadata } from 'next';

import { Check, Minus } from 'lucide-react';

import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { getAdminUsers } from '@/features/admin/api';
import { LoadError } from '@/features/admin/components/load-error';
import { RoleBadge, UserStatusBadge } from '@/features/admin/components/user-badges';
import { UserRowActions } from '@/features/admin/components/user-row-actions';
import { UsersPagination } from '@/features/admin/components/users-pagination';
import { UsersToolbar } from '@/features/admin/components/users-toolbar';
import { parseUserFilter } from '@/features/admin/filter-url';
import { canManageUsers } from '@/lib/auth/roles';
import { getCurrentUser } from '@/lib/auth/session';
import { formatDateTime } from '@/lib/format';

export const metadata: Metadata = { title: 'Người dùng' };

export default async function AdminUsersPage({ searchParams }: PageProps<'/admin/users'>) {
  const objFilter = parseUserFilter(await searchParams);
  const [objPage, objMe] = await Promise.all([getAdminUsers(objFilter), getCurrentUser()]);
  const bolCanManage = !!objMe && canManageUsers(objMe.roleLevel);

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Người dùng</h1>
        <p className="mt-1 text-sm text-ink-muted">
          {bolCanManage ? 'Quản lý tài khoản, vai trò và phiên đăng nhập' : 'Bạn chỉ có quyền xem danh sách'}
        </p>
      </div>
      {/* key: đổi bộ lọc từ link (vd phân trang) thì ô tìm kiếm lấy lại giá trị từ URL */}
      <UsersToolbar
        key={`${objFilter.keyword}|${objFilter.roleLevel}`}
        keyword={objFilter.keyword}
        roleLevel={objFilter.roleLevel}
      />

      {objPage ? (
        <>
          <div className="overflow-hidden rounded-2xl bg-white ring-1 ring-line">
            <Table>
              <TableHeader>
                <TableRow className="border-line bg-paper/60 hover:bg-paper/60 [&>th]:text-xs [&>th]:font-semibold [&>th]:text-ink-muted [&>th]:uppercase">
                  <TableHead className="pl-5">Người dùng</TableHead>
                  <TableHead className="hidden md:table-cell">Mã</TableHead>
                  <TableHead className="hidden sm:table-cell">Vai trò</TableHead>
                  <TableHead className="hidden sm:table-cell">Trạng thái</TableHead>
                  <TableHead className="hidden lg:table-cell">Google</TableHead>
                  <TableHead className="hidden xl:table-cell">Ngày tạo</TableHead>
                  <TableHead className="w-12 pr-5">
                    <span className="sr-only">Thao tác</span>
                  </TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {objPage.items.map((x) => (
                  <TableRow key={x.id} className="border-line hover:bg-paper/50">
                    <TableCell className="max-w-0 pl-5 sm:max-w-none">
                      <div className="flex min-w-0 items-center gap-3">
                        <span className="flex size-9 shrink-0 items-center justify-center rounded-full bg-paper text-sm font-semibold uppercase">
                          {(x.fullName || x.email).slice(0, 1)}
                        </span>
                        <div className="min-w-0">
                          <p className="truncate font-medium">
                            {x.fullName || <span className="text-ink-muted italic">Chưa có tên</span>}
                            {x.id === objMe?.id && (
                              <span className="ml-1.5 text-xs font-normal text-ink-muted">(bạn)</span>
                            )}
                          </p>
                          <p className="truncate text-xs text-ink-muted">{x.email}</p>
                          {/* Mobile ẩn cột vai trò/trạng thái → hiện gọn dưới email */}
                          <div className="mt-1.5 flex flex-wrap gap-1.5 sm:hidden">
                            <RoleBadge roleLevel={x.roleLevel} />
                            <UserStatusBadge user={x} />
                          </div>
                        </div>
                      </div>
                    </TableCell>
                    <TableCell className="hidden font-mono text-xs md:table-cell">{x.username}</TableCell>
                    <TableCell className="hidden sm:table-cell">
                      <RoleBadge roleLevel={x.roleLevel} />
                    </TableCell>
                    <TableCell className="hidden sm:table-cell">
                      <UserStatusBadge user={x} />
                    </TableCell>
                    <TableCell className="hidden lg:table-cell">
                      {x.hasGoogleLinked ? (
                        <Check aria-label="Đã liên kết" className="size-4 text-emerald-600" />
                      ) : (
                        <Minus aria-label="Chưa liên kết" className="size-4 text-ink-muted/50" />
                      )}
                    </TableCell>
                    <TableCell className="hidden text-ink-muted xl:table-cell">
                      {formatDateTime(x.createdDate)}
                    </TableCell>
                    <TableCell className="pr-5 text-right">
                      <UserRowActions user={x} canManage={bolCanManage && x.id !== objMe?.id} />
                    </TableCell>
                  </TableRow>
                ))}
                {objPage.items.length === 0 && (
                  <TableRow className="border-line hover:bg-transparent">
                    <TableCell colSpan={7} className="py-10 text-center text-ink-muted">
                      Không tìm thấy người dùng phù hợp.
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </div>
          <UsersPagination filter={objFilter} total={objPage.total} totalPages={objPage.totalPages} />
        </>
      ) : (
        <LoadError />
      )}
    </div>
  );
}
