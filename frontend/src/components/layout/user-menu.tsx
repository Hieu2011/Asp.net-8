'use client';

import Link from 'next/link';
import { useState } from 'react';

import { LayoutDashboard, LogOut, Package, ScanQrCode, ShieldCheck, UserRound } from 'lucide-react';

import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { LogoutDialog } from '@/features/auth/components/logout-dialog';
import { QrScanDialog } from '@/features/auth/components/qr-scan-dialog';

type UserMenuProps = { displayName: string | null; showAdminLink?: boolean };

export function UserMenu({ displayName, showAdminLink = false }: UserMenuProps) {
  const [bolLogoutOpen, setBolLogoutOpen] = useState(false);
  const [bolScanOpen, setBolScanOpen] = useState(false);

  if (!displayName) {
    return (
      <Link
        href="/login"
        className="inline-flex h-10 items-center gap-2 rounded-full px-3 text-sm text-slate-100 hover:bg-white/10 focus-visible:ring-2 focus-visible:ring-lamp focus-visible:outline-none"
      >
        <UserRound className="size-5" />
        <span className="hidden sm:inline">Đăng nhập</span>
      </Link>
    );
  }

  return (
    <>
      <DropdownMenu>
        <DropdownMenuTrigger
          aria-label={`Tài khoản ${displayName}`}
          className="inline-flex h-10 items-center gap-2 rounded-full px-3 text-sm text-slate-100 hover:bg-white/10 focus-visible:ring-2 focus-visible:ring-lamp focus-visible:outline-none"
        >
          <span className="flex size-7 items-center justify-center rounded-full bg-lamp text-xs font-bold text-night uppercase">
            {displayName.slice(0, 1)}
          </span>
          <span className="hidden max-w-28 truncate sm:inline">{displayName}</span>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end" className="w-56">
          {/* Base UI bắt buộc Label nằm trong Group */}
          <DropdownMenuGroup>
            <DropdownMenuLabel className="truncate">Xin chào, {displayName}</DropdownMenuLabel>
          </DropdownMenuGroup>
          <DropdownMenuSeparator />
          {showAdminLink && (
            <DropdownMenuItem render={<Link href="/admin" />}>
              <LayoutDashboard /> Trang quản trị
            </DropdownMenuItem>
          )}
          <DropdownMenuItem render={<Link href="/account" />}>
            <UserRound /> Tài khoản
          </DropdownMenuItem>
          <DropdownMenuItem render={<Link href="/account#sessions" />}>
            <ShieldCheck /> Phiên đăng nhập
          </DropdownMenuItem>
          <DropdownMenuItem onClick={() => setBolScanOpen(true)}>
            <ScanQrCode /> Quét mã QR đăng nhập
          </DropdownMenuItem>
          <DropdownMenuItem render={<Link href="/orders" />}>
            <Package /> Đơn hàng
          </DropdownMenuItem>
          <DropdownMenuSeparator />
          <DropdownMenuItem onClick={() => setBolLogoutOpen(true)}>
            <LogOut /> Đăng xuất
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>
      <LogoutDialog open={bolLogoutOpen} onOpenChange={setBolLogoutOpen} />
      <QrScanDialog open={bolScanOpen} onOpenChange={setBolScanOpen} />
    </>
  );
}
