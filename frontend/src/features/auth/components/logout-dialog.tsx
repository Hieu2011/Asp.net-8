'use client';

import { useRouter } from 'next/navigation';
import { useTransition } from 'react';

import { toast } from 'sonner';

import { ConfirmDialog } from '@/components/common/confirm-dialog';

import { logoutAction } from '../actions';
import { describeAuthError } from '../error-message';

type LogoutDialogProps = { open: boolean; onOpenChange: (bolOpen: boolean) => void };

export function LogoutDialog({ open, onOpenChange }: LogoutDialogProps) {
  const objRouter = useRouter();
  const [bolPending, startTransition] = useTransition();

  const handleConfirm = () =>
    startTransition(async () => {
      const objResult = await logoutAction();
      if (!objResult.ok) {
        toast.error('Đăng xuất thất bại', { description: describeAuthError(objResult) });
        return;
      }
      onOpenChange(false);
      toast.success('Đã đăng xuất');
      objRouter.replace('/login');
      objRouter.refresh();
    });

  return (
    <ConfirmDialog
      open={open}
      title="Đăng xuất?"
      description="Chỉ đăng xuất trên thiết bị này, các thiết bị khác vẫn giữ đăng nhập."
      confirmLabel="Đăng xuất"
      danger
      pending={bolPending}
      onConfirm={handleConfirm}
      onOpenChange={onOpenChange}
    />
  );
}
