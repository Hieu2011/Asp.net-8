'use client';

import Script from 'next/script';
import { useEffect, useRef, useState } from 'react';

import { clientEnv } from '@/lib/env.client';

type TurnstileApi = {
  render: (objElement: HTMLElement, objOption: Record<string, unknown>) => string;
  remove: (strWidgetID: string) => void;
};

declare global {
  interface Window {
    turnstile?: TurnstileApi;
  }
}

type TurnstileWidgetProps = { onToken: (strToken: string) => void };

// Widget 'normal' rộng cố định 300px; hẹp hơn thì dùng bản 'compact' (150px) để không tràn khung
const MIN_FLEXIBLE_WIDTH = 300;

/** Cloudflare Turnstile — chống bot, backend verify token bằng SecretKey */
export function TurnstileWidget({ onToken }: TurnstileWidgetProps) {
  const objContainerRef = useRef<HTMLDivElement>(null);
  const objOnTokenRef = useRef(onToken);
  const [bolScriptReady, setBolScriptReady] = useState(false);
  const [bolRendered, setBolRendered] = useState(false);

  useEffect(() => {
    objOnTokenRef.current = onToken;
  }, [onToken]);

  // Render 1 lần khi script sẵn sàng, gỡ widget khi unmount
  useEffect(() => {
    if (!bolScriptReady || !window.turnstile || !objContainerRef.current) return;
    const bolCompact = objContainerRef.current.offsetWidth < MIN_FLEXIBLE_WIDTH;
    const strWidgetID = window.turnstile.render(objContainerRef.current, {
      sitekey: clientEnv.NEXT_PUBLIC_TURNSTILE_SITE_KEY,
      theme: 'dark',
      language: 'vi',
      size: bolCompact ? 'compact' : 'flexible',
      callback: (strToken: string) => objOnTokenRef.current(strToken),
      'expired-callback': () => objOnTokenRef.current(''),
      'error-callback': () => objOnTokenRef.current(''),
    });
    setBolRendered(true);
    return () => {
      window.turnstile?.remove(strWidgetID);
      setBolRendered(false);
    };
  }, [bolScriptReady]);

  if (!clientEnv.NEXT_PUBLIC_TURNSTILE_SITE_KEY) {
    return <p className="text-xs text-amber-300">Chưa cấu hình NEXT_PUBLIC_TURNSTILE_SITE_KEY</p>;
  }
  return (
    <>
      <Script
        src="https://challenges.cloudflare.com/turnstile/v0/api.js?render=explicit"
        strategy="afterInteractive"
        onReady={() => setBolScriptReady(true)}
      />
      <div className="relative w-full">
        {!bolRendered && (
          <div
            aria-hidden
            className="absolute inset-0 flex h-[65px] items-center gap-3 rounded-lg border border-slate-700/70 bg-white/[0.02] px-4 text-sm text-slate-400"
          >
            <span className="size-5 animate-spin rounded-full border-2 border-slate-600 border-t-amber-200 motion-reduce:animate-none" />
            Đang kiểm tra bảo mật…
          </div>
        )}
        {/* Giữ chỗ đúng chiều cao widget để form không giật khi iframe tải xong */}
        <div ref={objContainerRef} className="flex min-h-[65px] w-full justify-center [&>div]:w-full" />
      </div>
    </>
  );
}
