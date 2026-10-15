import 'server-only';

import { callAuthApi } from '@/lib/auth/auth-api';
import { getAccessToken } from '@/lib/auth/session';

import { USERS_PAGE_SIZE } from './filter-url';
import type { UserFilterState } from './filter-url';
import type { AdminSession, AdminUser, PagedResult, UserStats } from './types';

async function getAdminData<T>(strPath: string): Promise<T | null> {
  const strAccessToken = await getAccessToken();
  if (!strAccessToken) return null;
  const objResult = await callAuthApi<T>(strPath, { method: 'GET', accessToken: strAccessToken });
  if (objResult.isError) console.error(`[admin] ${strPath} lỗi ${objResult.statusID}: ${objResult.message}`);
  return objResult.isError ? null : objResult.resultObject;
}

/** Danh sách user theo bộ lọc (null nếu lỗi/không đủ quyền) */
export function getAdminUsers(objFilter: UserFilterState) {
  const objParam = new URLSearchParams({ page: String(objFilter.page), pageSize: String(USERS_PAGE_SIZE) });
  if (objFilter.keyword) objParam.set('keyword', objFilter.keyword);
  if (objFilter.roleLevel) objParam.set('roleLevel', String(objFilter.roleLevel));
  return getAdminData<PagedResult<AdminUser>>(`/auth/users?${objParam}`);
}

export const getUserStats = () => getAdminData<UserStats>('/auth/admin/stats');

export const getActiveSessions = () => getAdminData<AdminSession[]>('/auth/admin/sessions');
