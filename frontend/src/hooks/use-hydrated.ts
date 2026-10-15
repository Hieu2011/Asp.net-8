import { useSyncExternalStore } from 'react';

const subscribe = () => () => {};

/** true khi đã chạy trên trình duyệt — tránh lệch HTML server/client với state từ localStorage */
export function useHydrated(): boolean {
  return useSyncExternalStore(
    subscribe,
    () => true,
    () => false,
  );
}
