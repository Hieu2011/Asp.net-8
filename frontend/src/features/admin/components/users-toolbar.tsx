'use client';

import { useRouter } from 'next/navigation';
import { useEffect, useRef, useState, useTransition } from 'react';

import { LoaderCircle, Search } from 'lucide-react';

import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { ROLE_OPTIONS } from '@/lib/auth/roles';

import { buildUsersHref } from '../filter-url';

type UsersToolbarProps = { keyword?: string; roleLevel?: number };

const ALL_ROLES = 'all';
const SEARCH_DELAY_MS = 400;
const ROLE_ITEMS = [
  { value: ALL_ROLES, label: 'Tất cả vai trò' },
  ...ROLE_OPTIONS.map((x) => ({ value: String(x.value), label: x.label })),
];

/** Tìm kiếm + lọc vai trò — ghi vào URL để F5/chia sẻ link vẫn giữ bộ lọc, trang server tự tải lại */
export function UsersToolbar({ keyword = '', roleLevel }: UsersToolbarProps) {
  const objRouter = useRouter();
  const [strKeyword, setStrKeyword] = useState(keyword);
  const [bolPending, startTransition] = useTransition();
  const objTimer = useRef<ReturnType<typeof setTimeout>>(undefined);

  const navigate = (strNextKeyword: string, intNextRole: number | undefined) =>
    startTransition(() =>
      objRouter.replace(
        buildUsersHref({ keyword: strNextKeyword.trim() || undefined, roleLevel: intNextRole, page: 1 }),
      ),
    );

  const handleKeywordChange = (strValue: string) => {
    setStrKeyword(strValue);
    clearTimeout(objTimer.current);
    objTimer.current = setTimeout(() => navigate(strValue, roleLevel), SEARCH_DELAY_MS);
  };

  const handleRoleChange = (strValue: string | null) => {
    clearTimeout(objTimer.current);
    navigate(strKeyword, !strValue || strValue === ALL_ROLES ? undefined : Number(strValue));
  };

  useEffect(() => () => clearTimeout(objTimer.current), []);

  return (
    <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
      <label className="relative block flex-1 sm:max-w-sm">
        <span className="sr-only">Tìm người dùng</span>
        <Search aria-hidden className="absolute top-1/2 left-3 size-4 -translate-y-1/2 text-ink-muted" />
        <input
          type="search"
          value={strKeyword}
          maxLength={100}
          onChange={(e) => handleKeywordChange(e.target.value)}
          onKeyDown={(e) => {
            if (e.key !== 'Enter') return;
            clearTimeout(objTimer.current);
            navigate(strKeyword, roleLevel);
          }}
          placeholder="Tìm theo email, họ tên, mã người dùng…"
          className="h-10 w-full rounded-lg border border-line bg-white pr-9 pl-9 text-sm placeholder:text-ink-muted focus:border-ink/40 focus:outline-none"
        />
        {bolPending && (
          <LoaderCircle
            aria-label="Đang tải"
            className="absolute top-1/2 right-3 size-4 -translate-y-1/2 animate-spin text-ink-muted"
          />
        )}
      </label>
      <Select items={ROLE_ITEMS} value={roleLevel ? String(roleLevel) : ALL_ROLES} onValueChange={handleRoleChange}>
        <SelectTrigger
          aria-label="Lọc theo vai trò"
          className="h-10 w-full border-line bg-white text-ink hover:bg-white sm:w-48 dark:bg-white dark:hover:bg-white"
        >
          <SelectValue />
        </SelectTrigger>
        <SelectContent>
          {ROLE_ITEMS.map((x) => (
            <SelectItem key={x.value} value={x.value}>
              {x.label}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
    </div>
  );
}
