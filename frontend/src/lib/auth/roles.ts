// Khớp enum RoleLevel backend — số càng nhỏ quyền càng cao
export const ROLE_LEVEL = { Admin: 1, Director: 2, Manager: 3, Staff: 4, Customer: 5 } as const;

export const ROLE_NAME: Record<number, string> = {
  [ROLE_LEVEL.Admin]: 'Quản trị',
  [ROLE_LEVEL.Director]: 'Giám đốc',
  [ROLE_LEVEL.Manager]: 'Quản lý',
  [ROLE_LEVEL.Staff]: 'Nhân viên',
  [ROLE_LEVEL.Customer]: 'Khách hàng',
};

export const ROLE_OPTIONS = Object.values(ROLE_LEVEL).map((x) => ({ value: x, label: ROLE_NAME[x] ?? `Cấp ${x}` }));

export const getRoleName = (intRoleLevel: number) => ROLE_NAME[intRoleLevel] ?? `Cấp ${intRoleLevel}`;

/** Được vào trang quản trị (Manager trở lên) — backend vẫn tự kiểm tra từng API */
export const canViewAdmin = (intRoleLevel: number) => intRoleLevel <= ROLE_LEVEL.Manager;

/** Được thao tác đổi quyền/khóa/xóa user */
export const canManageUsers = (intRoleLevel: number) => intRoleLevel === ROLE_LEVEL.Admin;
