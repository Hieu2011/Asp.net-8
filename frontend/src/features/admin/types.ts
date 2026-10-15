// Hợp đồng JSON với AuthService (camelCase) — giữ đúng tên field backend
import type { SessionInfo, UserProfile } from '@/features/auth/types';

export type AdminUser = UserProfile & { isPermanentlyLocked: boolean };

export type PagedResult<T> = {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
};

export type UserStats = {
  totalUsers: number;
  activeUsers: number;
  disabledUsers: number;
  lockedUsers: number;
  googleLinkedUsers: number;
  newUsers7Days: number;
};

export type AdminSession = SessionInfo & { userId: string; username: string | null };
