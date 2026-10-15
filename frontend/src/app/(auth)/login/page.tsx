import type { Metadata } from 'next';

import { LoginForm } from '@/features/auth/components/login-form';

export const metadata: Metadata = { title: 'Đăng nhập' };

const LOCAL_ORIGIN = 'http://local.invalid';

// Chỉ cho quay lại đường dẫn nội bộ — chặn open redirect: //web-la.com, /\web-la.com, ký tự điều khiển (trình duyệt bỏ TAB).
// Kiểm tra trên KẾT QUẢ đã chuẩn hóa: /..//web-la.com sau khi bỏ dot-segment thành //web-la.com
function toSafePath(strNext: unknown): string {
  if (typeof strNext !== 'string' || !strNext.startsWith('/') || strNext.includes('\\')) return '/';
  if ([...strNext].some((x) => x.charCodeAt(0) < 32 || x.charCodeAt(0) === 127)) return '/';
  const objUrl = new URL(strNext, LOCAL_ORIGIN);
  const strPath = `${objUrl.pathname}${objUrl.search}${objUrl.hash}`;
  return objUrl.origin === LOCAL_ORIGIN && !strPath.startsWith('//') ? strPath : '/';
}

export default async function LoginPage({ searchParams }: PageProps<'/login'>) {
  const { next } = await searchParams;
  return <LoginForm nextPath={toSafePath(next)} />;
}
