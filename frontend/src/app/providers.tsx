'use client';

import { useState } from 'react';
import type { ReactNode } from 'react';

import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ThemeProvider } from 'next-themes';

import { Toaster } from '@/components/ui/sonner';

export function Providers({ children }: { children: ReactNode }) {
  // Tạo QueryClient trong state → mỗi request SSR 1 instance riêng, không dùng chung giữa người dùng
  const [objQueryClient] = useState(
    () => new QueryClient({ defaultOptions: { queries: { staleTime: 60_000, refetchOnWindowFocus: false } } }),
  );
  return (
    <QueryClientProvider client={objQueryClient}>
      <ThemeProvider attribute="class" defaultTheme="system" enableSystem disableTransitionOnChange>
        {children}
        <Toaster
          position="top-center"
          richColors
          closeButton
          containerAriaLabel="Thông báo"
          toastOptions={{ closeButtonAriaLabel: 'Đóng thông báo' }}
        />
      </ThemeProvider>
    </QueryClientProvider>
  );
}
