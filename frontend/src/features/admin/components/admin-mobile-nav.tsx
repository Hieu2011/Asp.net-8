'use client';

import { useState } from 'react';

import { Menu } from 'lucide-react';

import { Sheet, SheetContent, SheetHeader, SheetTitle, SheetTrigger } from '@/components/ui/sheet';

import { AdminNavLinks } from './admin-nav-links';

export function AdminMobileNav() {
  const [bolOpen, setBolOpen] = useState(false);

  return (
    <Sheet open={bolOpen} onOpenChange={setBolOpen}>
      <SheetTrigger
        aria-label="Mở menu quản trị"
        className="inline-flex size-10 items-center justify-center rounded-lg text-ink hover:bg-paper lg:hidden"
      >
        <Menu className="size-5" />
      </SheetTrigger>
      <SheetContent side="left" className="w-72 border-r-0 bg-night text-slate-100">
        <SheetHeader>
          <SheetTitle className="text-slate-100">Quản trị</SheetTitle>
        </SheetHeader>
        <nav aria-label="Menu quản trị" className="px-3">
          <AdminNavLinks onNavigate={() => setBolOpen(false)} />
        </nav>
      </SheetContent>
    </Sheet>
  );
}
