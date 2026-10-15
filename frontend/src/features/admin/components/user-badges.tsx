import { getRoleName, ROLE_LEVEL } from '@/lib/auth/roles';
import { cn } from '@/lib/utils';

import type { AdminUser } from '../types';

const BADGE_CLASS =
  'inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium whitespace-nowrap ring-1 ring-inset';

const ROLE_CLASS: Record<number, string> = {
  [ROLE_LEVEL.Admin]: 'bg-night text-lamp ring-night',
  [ROLE_LEVEL.Director]: 'bg-violet-50 text-violet-700 ring-violet-200',
  [ROLE_LEVEL.Manager]: 'bg-sky-50 text-sky-700 ring-sky-200',
  [ROLE_LEVEL.Staff]: 'bg-teal-50 text-teal-700 ring-teal-200',
};

export function RoleBadge({ roleLevel }: { roleLevel: number }) {
  return (
    <span className={cn(BADGE_CLASS, ROLE_CLASS[roleLevel] ?? 'bg-paper text-ink-muted ring-line')}>
      {getRoleName(roleLevel)}
    </span>
  );
}

/** Khóa vĩnh viễn ưu tiên hiển thị hơn vô hiệu hóa */
export function UserStatusBadge({ user }: { user: Pick<AdminUser, 'isActive' | 'isPermanentlyLocked'> }) {
  if (user.isPermanentlyLocked)
    return <span className={cn(BADGE_CLASS, 'bg-rose-50 text-rose-700 ring-rose-200')}>Khóa vĩnh viễn</span>;
  if (!user.isActive)
    return <span className={cn(BADGE_CLASS, 'bg-amber-50 text-amber-700 ring-amber-200')}>Vô hiệu hóa</span>;
  return <span className={cn(BADGE_CLASS, 'bg-emerald-50 text-emerald-700 ring-emerald-200')}>Hoạt động</span>;
}
