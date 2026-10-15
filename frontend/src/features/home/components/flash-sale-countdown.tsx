'use client';

import { useEffect, useState } from 'react';

import { useHydrated } from '@/hooks/use-hydrated';

// Flash sale kết thúc lúc nửa đêm hôm nay (giờ máy người dùng)
const getSecondsToMidnight = () => {
  const dtmNow = new Date();
  const dtmMidnight = new Date(dtmNow);
  dtmMidnight.setHours(24, 0, 0, 0);
  return Math.max(0, Math.floor((dtmMidnight.getTime() - dtmNow.getTime()) / 1000));
};

const pad = (intValue: number) => String(intValue).padStart(2, '0');

export function FlashSaleCountdown() {
  const bolHydrated = useHydrated();
  const [intSeconds, setIntSeconds] = useState(getSecondsToMidnight);

  useEffect(() => {
    const intTimer = window.setInterval(() => setIntSeconds(getSecondsToMidnight()), 1000);
    return () => window.clearInterval(intTimer);
  }, []);

  const lstPart = [Math.floor(intSeconds / 3600), Math.floor((intSeconds % 3600) / 60), intSeconds % 60];
  return (
    <p className="flex items-center gap-2 text-sm text-ink-muted">
      Kết thúc sau
      <span role="timer" aria-live="off" className="flex gap-1 font-mono text-sm font-semibold tabular-nums">
        {lstPart.map((x, i) => (
          <span key={i} className="rounded-md bg-night px-1.5 py-0.5 text-lamp-soft">
            {bolHydrated ? pad(x) : '--'}
          </span>
        ))}
      </span>
    </p>
  );
}
