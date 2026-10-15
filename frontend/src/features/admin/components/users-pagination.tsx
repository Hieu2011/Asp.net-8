import Link from 'next/link';

import { ChevronLeft, ChevronRight } from 'lucide-react';

import { formatNumber } from '@/lib/format';
import { cn } from '@/lib/utils';

import { buildUsersHref } from '../filter-url';
import type { UserFilterState } from '../filter-url';

type UsersPaginationProps = { filter: UserFilterState; total: number; totalPages: number };

export function UsersPagination({ filter, total, totalPages }: UsersPaginationProps) {
  const strLinkClass = 'inline-flex h-9 items-center gap-1 rounded-lg px-3 text-sm ring-1 ring-line';
  const renderLink = (intPage: number, bolEnabled: boolean, strLabel: string, bolNext: boolean) =>
    bolEnabled ? (
      <Link href={buildUsersHref({ ...filter, page: intPage })} className={cn(strLinkClass, 'bg-white hover:bg-paper')}>
        {!bolNext && <ChevronLeft aria-hidden className="size-4" />}
        {strLabel}
        {bolNext && <ChevronRight aria-hidden className="size-4" />}
      </Link>
    ) : (
      <span aria-disabled className={cn(strLinkClass, 'cursor-not-allowed text-ink-muted/60')}>
        {!bolNext && <ChevronLeft aria-hidden className="size-4" />}
        {strLabel}
        {bolNext && <ChevronRight aria-hidden className="size-4" />}
      </span>
    );

  return (
    <nav aria-label="Phân trang" className="flex flex-wrap items-center justify-between gap-3">
      <p className="text-sm text-ink-muted">
        {formatNumber(total)} người dùng · Trang {filter.page}/{Math.max(totalPages, 1)}
      </p>
      <div className="flex gap-2">
        {renderLink(filter.page - 1, filter.page > 1, 'Trước', false)}
        {renderLink(filter.page + 1, filter.page < totalPages, 'Sau', true)}
      </div>
    </nav>
  );
}
