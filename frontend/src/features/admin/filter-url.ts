import { ROLE_LEVEL } from '@/lib/auth/roles';

export const USERS_PAGE_SIZE = 20;

export type UserFilterState = { keyword?: string; roleLevel?: number; page: number };

type SearchParamValue = string | string[] | undefined;

const firstValue = (objValue: SearchParamValue) => (Array.isArray(objValue) ? objValue[0] : objValue);

const VALID_ROLES = new Set<number>(Object.values(ROLE_LEVEL));

/** Đọc bộ lọc từ URL — giá trị lạ thì bỏ qua thay vì báo lỗi */
export function parseUserFilter(dicParam: Record<string, SearchParamValue>): UserFilterState {
  const intRole = Number(firstValue(dicParam.role));
  const intPage = Math.floor(Number(firstValue(dicParam.page)));
  return {
    keyword: firstValue(dicParam.q)?.trim().slice(0, 100) || undefined,
    roleLevel: VALID_ROLES.has(intRole) ? intRole : undefined,
    page: intPage > 0 ? intPage : 1,
  };
}

export function buildUsersHref(objState: UserFilterState) {
  const objParam = new URLSearchParams();
  if (objState.keyword) objParam.set('q', objState.keyword);
  if (objState.roleLevel) objParam.set('role', String(objState.roleLevel));
  if (objState.page > 1) objParam.set('page', String(objState.page));
  const strQuery = objParam.toString();
  return strQuery ? `/admin/users?${strQuery}` : '/admin/users';
}
