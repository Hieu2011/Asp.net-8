import type { ReactNode } from 'react';

import { Button } from '@/components/ui/button';

type AuthSubmitButtonProps = { pending: boolean; children: ReactNode };

export function AuthSubmitButton({ pending, children }: AuthSubmitButtonProps) {
  return (
    <Button
      type="submit"
      disabled={pending}
      className="h-11 w-full rounded-full bg-amber-200 text-base font-semibold text-slate-900 hover:bg-amber-100"
    >
      {pending ? 'Đang xử lý…' : children}
    </Button>
  );
}
