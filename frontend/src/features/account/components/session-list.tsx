'use client';

import { useRouter } from 'next/navigation';
import { useState, useTransition } from 'react';

import { Monitor, Smartphone } from 'lucide-react';
import { toast } from 'sonner';

import { ConfirmDialog } from '@/components/common/confirm-dialog';
import { Button } from '@/components/ui/button';
import { describeAuthError } from '@/features/auth/error-message';
import type { SessionInfo } from '@/features/auth/types';
import { describeDevice, isMobileDevice } from '@/lib/device';
import { formatDateTime } from '@/lib/format';

import { revokeAllSessionsAction, revokeSessionAction } from '../actions';

type SessionListProps = { sessions: SessionInfo[] };
type ConfirmState = { kind: 'one'; session: SessionInfo } | { kind: 'all' } | null;

export function SessionList({ sessions }: SessionListProps) {
  const objRouter = useRouter();
  const [objConfirm, setObjConfirm] = useState<ConfirmState>(null);
  const [bolPending, startTransition] = useTransition();
  const lstActive = sessions.filter((x) => !x.isRevoked);

  const handleRevoke = (objSession: SessionInfo) =>
    startTransition(async () => {
      const objResult = await revokeSessionAction(objSession.id);
      if (!objResult.ok) {
        toast.error('Đăng xuất phiên thất bại', { description: describeAuthError(objResult) });
        return;
      }
      setObjConfirm(null);
      toast.success(`Đã đăng xuất ${describeDevice(objSession.deviceInfo)}`);
    });

  const handleRevokeAll = () =>
    startTransition(async () => {
      const objResult = await revokeAllSessionsAction();
      if (!objResult.ok) {
        toast.error('Đăng xuất tất cả thiết bị thất bại', { description: describeAuthError(objResult) });
        return;
      }
      setObjConfirm(null);
      toast.success('Đã đăng xuất khỏi tất cả thiết bị');
      objRouter.replace('/login');
      objRouter.refresh();
    });

  return (
    <div className="space-y-4">
      <ul className="divide-y divide-line rounded-2xl bg-white ring-1 ring-line">
        {lstActive.map((x) => {
          const DeviceIcon = isMobileDevice(x.deviceInfo) ? Smartphone : Monitor;
          return (
            <li key={x.id} className="flex items-center gap-4 p-4">
              <span className="flex size-10 shrink-0 items-center justify-center rounded-xl bg-paper">
                <DeviceIcon aria-hidden className="size-5 text-ink-muted" />
              </span>
              <div className="min-w-0 flex-1 text-sm">
                <p className="font-medium">{describeDevice(x.deviceInfo)}</p>
                <p className="text-ink-muted">
                  IP {x.ipAddress ?? 'không rõ'}, hoạt động lúc {formatDateTime(x.lastActivityAt)}
                </p>
              </div>
              <Button
                variant="outline"
                size="sm"
                disabled={bolPending}
                onClick={() => setObjConfirm({ kind: 'one', session: x })}
              >
                Đăng xuất
              </Button>
            </li>
          );
        })}
        {lstActive.length === 0 && <li className="p-4 text-sm text-ink-muted">Không có phiên nào đang hoạt động.</li>}
      </ul>
      {lstActive.length > 1 && (
        <Button variant="destructive" disabled={bolPending} onClick={() => setObjConfirm({ kind: 'all' })}>
          Đăng xuất khỏi tất cả thiết bị
        </Button>
      )}

      <ConfirmDialog
        open={objConfirm?.kind === 'one'}
        title="Đăng xuất phiên này?"
        description={
          objConfirm?.kind === 'one'
            ? `${describeDevice(objConfirm.session.deviceInfo)} (IP ${objConfirm.session.ipAddress ?? 'không rõ'}) sẽ bị đăng xuất ngay. Nếu đây là thiết bị bạn đang dùng, bạn cần đăng nhập lại.`
            : ''
        }
        confirmLabel="Đăng xuất"
        danger
        pending={bolPending}
        onConfirm={() => objConfirm?.kind === 'one' && handleRevoke(objConfirm.session)}
        onOpenChange={(bolOpen) => !bolOpen && setObjConfirm(null)}
      />
      <ConfirmDialog
        open={objConfirm?.kind === 'all'}
        title="Đăng xuất khỏi tất cả thiết bị?"
        description="Mọi thiết bị, kể cả thiết bị này, sẽ bị đăng xuất ngay. Bạn cần đăng nhập lại để tiếp tục."
        confirmLabel="Đăng xuất tất cả"
        danger
        pending={bolPending}
        onConfirm={handleRevokeAll}
        onOpenChange={(bolOpen) => !bolOpen && setObjConfirm(null)}
      />
    </div>
  );
}
