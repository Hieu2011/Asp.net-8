'use client';

import { useState, useTransition } from 'react';

import { Ban, CircleCheck, Ellipsis, KeyRound, LockOpen, MonitorSmartphone, ShieldCheck, Trash2 } from 'lucide-react';
import { toast } from 'sonner';

import { ConfirmDialog } from '@/components/common/confirm-dialog';
import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { describeAuthError } from '@/features/auth/error-message';
import { ROLE_OPTIONS, getRoleName } from '@/lib/auth/roles';

import { changeUserRoleAction, runUserCommandAction } from '../actions';
import type { AdminUser } from '../types';
import { UserSessionsDialog } from './user-sessions-dialog';

type UserCommand = Parameters<typeof runUserCommandAction>[1];
type DialogState = { kind: 'confirm'; command: UserCommand } | { kind: 'role' } | { kind: 'sessions' } | null;
type UserRowActionsProps = { user: AdminUser; canManage: boolean };

const CONFIRM_TEXT: Record<
  UserCommand,
  { title: string; description: string; action: string; success: string; danger?: boolean }
> = {
  disable: {
    title: 'Vô hiệu hóa tài khoản?',
    description: 'Người dùng bị đăng xuất khỏi mọi thiết bị và không đăng nhập được cho tới khi được kích hoạt lại.',
    action: 'Vô hiệu hóa',
    success: 'Đã vô hiệu hóa tài khoản',
    danger: true,
  },
  enable: {
    title: 'Kích hoạt lại tài khoản?',
    description: 'Người dùng đăng nhập lại được như bình thường.',
    action: 'Kích hoạt',
    success: 'Đã kích hoạt lại tài khoản',
  },
  unlock: {
    title: 'Mở khóa tài khoản?',
    description: 'Gỡ khóa tạm (sai OTP/mật khẩu) và khóa vĩnh viễn nếu có, đặt lại số lần nhập sai.',
    action: 'Mở khóa',
    success: 'Đã mở khóa tài khoản',
  },
  delete: {
    title: 'Xóa tài khoản?',
    description:
      'Tài khoản bị xóa khỏi danh sách và đăng xuất khỏi mọi thiết bị. Email có thể được dùng để đăng ký lại.',
    action: 'Xóa tài khoản',
    success: 'Đã xóa tài khoản',
    danger: true,
  },
  'revoke-sessions': {
    title: 'Đăng xuất khỏi mọi thiết bị?',
    description: 'Mọi phiên đăng nhập của người dùng này bị thu hồi ngay.',
    action: 'Đăng xuất tất cả',
    success: 'Đã đăng xuất người dùng khỏi mọi thiết bị',
    danger: true,
  },
};

const ROLE_ITEMS = ROLE_OPTIONS.map((x) => ({ value: String(x.value), label: x.label }));

/** Menu thao tác trên 1 dòng user — Manager chỉ xem phiên, Admin thao tác đầy đủ (backend kiểm tra lại) */
export function UserRowActions({ user, canManage }: UserRowActionsProps) {
  const [objDialog, setObjDialog] = useState<DialogState>(null);
  const [strRole, setStrRole] = useState(String(user.roleLevel));
  const [bolPending, startTransition] = useTransition();
  const strName = user.fullName || user.email;

  const openDialog = (objNext: DialogState) => {
    setStrRole(String(user.roleLevel));
    setObjDialog(objNext);
  };

  const handleConfirm = (strCommand: UserCommand) =>
    startTransition(async () => {
      const objText = CONFIRM_TEXT[strCommand];
      const objResult = await runUserCommandAction(user.id, strCommand);
      if (!objResult.ok) {
        toast.error(`${objText.action} thất bại`, { description: describeAuthError(objResult) });
        return;
      }
      setObjDialog(null);
      toast.success(objText.success, { description: strName });
    });

  const handleChangeRole = () =>
    startTransition(async () => {
      const objResult = await changeUserRoleAction(user.id, Number(strRole));
      if (!objResult.ok) {
        toast.error('Đổi vai trò thất bại', { description: describeAuthError(objResult) });
        return;
      }
      setObjDialog(null);
      toast.success(`Đã đổi vai trò thành ${getRoleName(Number(strRole))}`, { description: strName });
    });

  const objConfirm = objDialog?.kind === 'confirm' ? CONFIRM_TEXT[objDialog.command] : null;

  return (
    <>
      <DropdownMenu>
        <DropdownMenuTrigger
          aria-label={`Thao tác với ${strName}`}
          className="inline-flex size-8 items-center justify-center rounded-lg text-ink-muted hover:bg-paper hover:text-ink"
        >
          <Ellipsis className="size-4" />
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end" className="w-56">
          <DropdownMenuItem onClick={() => openDialog({ kind: 'sessions' })}>
            <MonitorSmartphone /> Phiên đăng nhập
          </DropdownMenuItem>
          {canManage && (
            <>
              <DropdownMenuItem onClick={() => openDialog({ kind: 'role' })}>
                <ShieldCheck /> Đổi vai trò
              </DropdownMenuItem>
              <DropdownMenuItem onClick={() => openDialog({ kind: 'confirm', command: 'unlock' })}>
                {user.isPermanentlyLocked ? <LockOpen /> : <KeyRound />} Mở khóa
              </DropdownMenuItem>
              {user.isActive ? (
                <DropdownMenuItem onClick={() => openDialog({ kind: 'confirm', command: 'disable' })}>
                  <Ban /> Vô hiệu hóa
                </DropdownMenuItem>
              ) : (
                <DropdownMenuItem onClick={() => openDialog({ kind: 'confirm', command: 'enable' })}>
                  <CircleCheck /> Kích hoạt lại
                </DropdownMenuItem>
              )}
              <DropdownMenuSeparator />
              <DropdownMenuItem
                variant="destructive"
                onClick={() => openDialog({ kind: 'confirm', command: 'delete' })}
              >
                <Trash2 /> Xóa tài khoản
              </DropdownMenuItem>
            </>
          )}
        </DropdownMenuContent>
      </DropdownMenu>

      <ConfirmDialog
        open={objConfirm !== null}
        title={objConfirm?.title ?? ''}
        description={
          <>
            <span className="font-medium text-foreground">{strName}</span> — {objConfirm?.description}
          </>
        }
        confirmLabel={objConfirm?.action ?? ''}
        danger={objConfirm?.danger}
        pending={bolPending}
        onConfirm={() => objDialog?.kind === 'confirm' && handleConfirm(objDialog.command)}
        onOpenChange={(bolOpen) => !bolOpen && setObjDialog(null)}
      />

      <Dialog open={objDialog?.kind === 'role'} onOpenChange={(bolOpen) => !bolOpen && setObjDialog(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Đổi vai trò</DialogTitle>
            <DialogDescription>
              <span className="font-medium text-foreground">{strName}</span> sẽ bị đăng xuất khỏi mọi thiết bị để áp
              dụng quyền mới.
            </DialogDescription>
          </DialogHeader>
          <Select items={ROLE_ITEMS} value={strRole} onValueChange={(strValue) => strValue && setStrRole(strValue)}>
            <SelectTrigger aria-label="Vai trò mới" className="h-10 w-full">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {ROLE_ITEMS.map((x) => (
                <SelectItem key={x.value} value={x.value}>
                  {x.label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          <DialogFooter>
            <Button variant="outline" disabled={bolPending} onClick={() => setObjDialog(null)}>
              Hủy
            </Button>
            <Button disabled={bolPending || strRole === String(user.roleLevel)} onClick={handleChangeRole}>
              {bolPending ? 'Đang lưu…' : 'Lưu vai trò'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {objDialog?.kind === 'sessions' && (
        <UserSessionsDialog
          user={user}
          canManage={canManage}
          onClose={() => setObjDialog(null)}
          onRevokeAll={() => openDialog({ kind: 'confirm', command: 'revoke-sessions' })}
        />
      )}
    </>
  );
}
