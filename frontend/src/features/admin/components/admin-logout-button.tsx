'use client';

import { useState } from 'react';

import { LogOut } from 'lucide-react';

import { LogoutDialog } from '@/features/auth/components/logout-dialog';

export function AdminLogoutButton() {
  const [bolOpen, setBolOpen] = useState(false);

  return (
    <>
      <button
        type="button"
        aria-label="Đăng xuất"
        onClick={() => setBolOpen(true)}
        className="inline-flex size-9 items-center justify-center rounded-lg text-ink-muted hover:bg-paper hover:text-sale"
      >
        <LogOut className="size-4" />
      </button>
      <LogoutDialog open={bolOpen} onOpenChange={setBolOpen} />
    </>
  );
}
