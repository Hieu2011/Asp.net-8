'use client';

import { useEffect, useState, useTransition } from 'react';

import { LoaderCircle, Monitor, Smartphone } from 'lucide-react';
import { toast } from 'sonner';

import { ConfirmDialog } from '@/components/common/confirm-dialog';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { describeAuthError } from '@/features/auth/error-message';
import { describeDevice, isMobileDevice } from '@/lib/device';
import { formatDateTime } from '@/lib/format';

import { getUserSessionsAction, revokeUserSessionAction } from '../actions';
import type { AdminSession, AdminUser } from '../types';

type UserSessionsDialogProps = {
  user: AdminUser;
  canManage: boolean;
  onClose: () => void;
  onRevokeAll: () => void;
};

/** Danh sách phiên đang đăng nhập của 1 user — tải khi mở, Admin thu hồi được từng phiên */
export function UserSessionsDialog({ user, canManage, onClose, onRevokeAll }: UserSessionsDialogProps) {
  const [lstSession, setLstSession] = useState<AdminSession[] | null>(null);
  const [strError, setStrError] = useState('');
  const [objRevoking, setObjRevoking] = useState<AdminSession | null>(null);
  const [bolPending, startTransition] = useTransition();
  const lstActive = (lstSession ?? []).filter((x) => !x.isRevoked);

  const loadSessions = () =>
    startTransition(async () => {
      const objResult = await getUserSessionsAction(user.id);
      if (!objResult.ok) return setStrError(describeAuthError(objResult));
      setStrError('');
      setLstSession(objResult.data);
    });

  const handleRevoke = (objTarget: AdminSession) =>
    startTransition(async () => {
      const objResult = await revokeUserSessionAction(user.id, objTarget.id);
      if (!objResult.ok) {
        toast.error('Thu hồi phiên thất bại', { description: describeAuthError(objResult) });
        return;
      }
      setObjRevoking(null);
      setLstSession((x) => x?.filter((objSession) => objSession.id !== objTarget.id) ?? null);
      toast.success(`Đã thu hồi phiên ${describeDevice(objTarget.deviceInfo)}`);
    });

  // Chỉ tải 1 lần khi dialog mở (component được mount lúc mở)
  // eslint-disable-next-line react-hooks/exhaustive-deps
  useEffect(() => loadSessions(), []);

  return (
    <Dialog open onOpenChange={(bolOpen) => !bolOpen && onClose()}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>Phiên đăng nhập</DialogTitle>
          <DialogDescription className="break-all">{user.fullName || user.email}</DialogDescription>
        </DialogHeader>
        {lstSession === null && !strError ? (
          <p className="flex items-center gap-2 text-sm text-muted-foreground">
            <LoaderCircle aria-hidden className="size-4 animate-spin" /> Đang tải…
          </p>
        ) : (
          <ul className="max-h-80 divide-y overflow-y-auto rounded-lg ring-1 ring-foreground/10">
            {lstActive.map((x) => {
              const DeviceIcon = isMobileDevice(x.deviceInfo) ? Smartphone : Monitor;
              return (
                <li key={x.id} className="flex items-center gap-3 p-3">
                  <DeviceIcon aria-hidden className="size-5 shrink-0 text-muted-foreground" />
                  <div className="min-w-0 flex-1 text-sm">
                    <p className="font-medium">{describeDevice(x.deviceInfo)}</p>
                    <p className="truncate text-xs text-muted-foreground">
                      IP {x.ipAddress ?? 'không rõ'} · {formatDateTime(x.lastActivityAt)}
                    </p>
                  </div>
                  {canManage && (
                    <Button variant="outline" size="sm" disabled={bolPending} onClick={() => setObjRevoking(x)}>
                      Thu hồi
                    </Button>
                  )}
                </li>
              );
            })}
            {lstSession !== null && lstActive.length === 0 && (
              <li className="p-3 text-sm text-muted-foreground">Không có phiên nào đang hoạt động.</li>
            )}
          </ul>
        )}
        {strError && (
          <p role="alert" className="text-sm text-destructive">
            {strError}
          </p>
        )}
        {canManage && lstActive.length > 1 && (
          <Button variant="destructive" disabled={bolPending} onClick={onRevokeAll}>
            Đăng xuất khỏi tất cả thiết bị
          </Button>
        )}
        <ConfirmDialog
          open={objRevoking !== null}
          title="Thu hồi phiên này?"
          description={`${describeDevice(objRevoking?.deviceInfo ?? null)} (IP ${objRevoking?.ipAddress ?? 'không rõ'}) của ${user.fullName || user.email} sẽ bị đăng xuất ngay.`}
          confirmLabel="Thu hồi"
          danger
          pending={bolPending}
          onConfirm={() => objRevoking && handleRevoke(objRevoking)}
          onOpenChange={(bolOpen) => !bolOpen && setObjRevoking(null)}
        />
      </DialogContent>
    </Dialog>
  );
}
