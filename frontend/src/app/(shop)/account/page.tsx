import type { Metadata } from 'next';
import { redirect } from 'next/navigation';

import { getMySessions } from '@/features/account/api';
import { SessionList } from '@/features/account/components/session-list';
import { getRoleName } from '@/lib/auth/roles';
import { getCurrentUser } from '@/lib/auth/session';
import { formatDateTime } from '@/lib/format';

export const metadata: Metadata = { title: 'Tài khoản' };

export default async function AccountPage() {
  // Proxy chỉ check cookie — kiểm tra lại với AuthService ngay tại trang
  const objUser = await getCurrentUser();
  if (!objUser) redirect('/login?next=/account');
  const lstSession = (await getMySessions()) ?? [];

  const lstInfo = [
    { label: 'Họ tên', value: objUser.fullName || 'Chưa cập nhật' },
    { label: 'Email đăng nhập', value: objUser.email },
    { label: 'Mã người dùng', value: objUser.username },
    { label: 'Vai trò', value: getRoleName(objUser.roleLevel) },
    { label: 'Xác thực OTP', value: objUser.isTotpEnabled ? 'Đã bật' : 'Chưa bật' },
    { label: 'Liên kết Google', value: objUser.hasGoogleLinked ? 'Đã liên kết' : 'Chưa liên kết' },
    { label: 'Ngày tạo', value: formatDateTime(objUser.createdDate) },
  ];

  return (
    <div className="mx-auto max-w-4xl space-y-12 px-4 py-8">
      <section aria-labelledby="profile-heading">
        <h1 id="profile-heading" className="text-3xl font-bold tracking-tight">
          Tài khoản của bạn
        </h1>
        <dl className="mt-5 grid gap-px overflow-hidden rounded-2xl bg-line ring-1 ring-line sm:grid-cols-2">
          {lstInfo.map((x) => (
            <div key={x.label} className="bg-white p-4">
              <dt className="text-sm text-ink-muted">{x.label}</dt>
              <dd className="mt-0.5 font-medium">{x.value}</dd>
            </div>
          ))}
        </dl>
      </section>

      <section id="sessions" aria-labelledby="sessions-heading" className="scroll-mt-28">
        <h2 id="sessions-heading" className="text-2xl font-bold tracking-tight">
          Phiên đăng nhập
        </h2>
        <p className="mt-1 mb-5 text-sm text-ink-muted">Thấy thiết bị lạ? Đăng xuất phiên đó ngay và đổi mật khẩu.</p>
        <SessionList sessions={lstSession} />
      </section>
    </div>
  );
}
