import type { Metadata } from 'next';
import Link from 'next/link';
import { redirect } from 'next/navigation';

import { TriangleAlert } from 'lucide-react';

import { Logo } from '@/components/layout/logo';
import { QrApproveCard } from '@/features/auth/components/qr-approve-card';
import { scanQrChallenge } from '@/features/auth/qr-api';
import { getCurrentUser } from '@/lib/auth/session';

export const metadata: Metadata = { title: 'Xác nhận đăng nhập', robots: { index: false } };

export default async function QrLoginConfirmPage({ params }: PageProps<'/qr-login/[challengeId]'>) {
  const { challengeId } = await params;
  // Proxy chỉ check cookie — chưa đăng nhập thật thì đăng nhập rồi quay lại đúng trang này
  const objUser = await getCurrentUser();
  if (!objUser) redirect(`/login?next=/qr-login/${encodeURIComponent(challengeId)}`);
  const objScan = await scanQrChallenge(challengeId);

  return (
    <main className="flex min-h-dvh flex-col items-center bg-night px-4 py-10 text-slate-100">
      <Logo />
      <div className="mt-10 w-full max-w-sm rounded-2xl bg-night-soft p-6 ring-1 ring-white/10">
        {objScan.ok ? (
          <QrApproveCard
            challengeID={challengeId}
            info={objScan.data}
            accountLabel={objUser.fullName || objUser.email}
          />
        ) : (
          <div className="space-y-4 text-center">
            <TriangleAlert aria-hidden className="mx-auto size-12 text-amber-300" />
            <h1 className="text-xl font-semibold">Không dùng được mã này</h1>
            <p className="text-sm text-slate-400">{objScan.message}</p>
            <Link href="/" className="inline-block text-sm font-medium text-amber-200 hover:text-amber-100">
              Về trang chủ
            </Link>
          </div>
        )}
      </div>
    </main>
  );
}
