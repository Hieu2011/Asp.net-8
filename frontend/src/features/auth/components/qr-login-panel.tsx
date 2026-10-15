'use client';

import { useCallback, useEffect, useRef, useState } from 'react';

import { CircleCheck, LoaderCircle, RefreshCw, Smartphone } from 'lucide-react';
import { QRCodeSVG } from 'qrcode.react';

import { describeAuthError } from '../error-message';
import { pollQrLoginAction, startQrLoginAction } from '../qr-actions';

type QrLoginPanelProps = { onSuccess: () => void };
type PanelState =
  | { kind: 'loading' }
  | { kind: 'ready'; challengeID: string; expiresAt: number; pollInterval: number; scanned: boolean }
  | { kind: 'expired'; message: string }
  | { kind: 'error'; message: string }
  | { kind: 'approved' };

/** Máy tính hiện QR → điện thoại đã đăng nhập quét + đồng ý → máy tính tự vào (hỏi trạng thái định kỳ) */
export function QrLoginPanel({ onSuccess }: QrLoginPanelProps) {
  const [objState, setObjState] = useState<PanelState>({ kind: 'loading' });
  const [intSecondsLeft, setIntSecondsLeft] = useState(0);
  const objPollTimer = useRef<ReturnType<typeof setTimeout>>(undefined);

  const startChallenge = useCallback(async () => {
    clearTimeout(objPollTimer.current);
    setObjState({ kind: 'loading' });
    const objResult = await startQrLoginAction();
    if (!objResult.ok) return setObjState({ kind: 'error', message: describeAuthError(objResult) });
    const { challengeId, expiresIn, pollInterval } = objResult.data;
    setObjState({
      kind: 'ready',
      challengeID: challengeId,
      expiresAt: Date.now() + expiresIn * 1000,
      pollInterval,
      scanned: false,
    });
  }, []);

  const handleRefresh = () => void startChallenge();

  useEffect(() => {
    // Gọi server action lúc mount — setState nằm trong callback bất đồng bộ
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void startChallenge();
    return () => clearTimeout(objPollTimer.current);
  }, [startChallenge]);

  // Hỏi trạng thái định kỳ; tab đang ẩn thì bỏ qua lượt đó cho đỡ tốn request
  useEffect(() => {
    if (objState.kind !== 'ready') return;
    let bolCancelled = false;
    const poll = async () => {
      if (document.visibilityState === 'visible') {
        const objResult = await pollQrLoginAction();
        if (bolCancelled) return;
        if (!objResult.ok) return setObjState({ kind: 'error', message: describeAuthError(objResult) });
        const { status } = objResult.data;
        if (status === 'approved') {
          setObjState({ kind: 'approved' });
          return onSuccess();
        }
        if (status === 'rejected')
          return setObjState({ kind: 'expired', message: 'Yêu cầu đăng nhập đã bị từ chối trên điện thoại.' });
        if (status === 'expired') return setObjState({ kind: 'expired', message: 'Mã QR đã hết hạn.' });
        if (status === 'scanned') setObjState((x) => (x.kind === 'ready' && !x.scanned ? { ...x, scanned: true } : x));
      }
      objPollTimer.current = setTimeout(poll, objState.pollInterval * 1000);
    };
    objPollTimer.current = setTimeout(poll, objState.pollInterval * 1000);
    return () => {
      bolCancelled = true;
      clearTimeout(objPollTimer.current);
    };
    // Chỉ khởi động lại vòng hỏi khi đổi mã QR
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [objState.kind === 'ready' ? objState.challengeID : objState.kind]);

  // Đếm ngược hiển thị; hết giờ thì chuyển hết hạn mà không cần chờ lượt hỏi kế tiếp
  useEffect(() => {
    if (objState.kind !== 'ready') return;
    const tick = () => {
      const intLeft = Math.max(0, Math.ceil((objState.expiresAt - Date.now()) / 1000));
      setIntSecondsLeft(intLeft);
      if (intLeft === 0) setObjState({ kind: 'expired', message: 'Mã QR đã hết hạn.' });
    };
    tick();
    const intTimer = setInterval(tick, 1000);
    return () => clearInterval(intTimer);
  }, [objState]);

  const strQrValue =
    objState.kind === 'ready' && typeof window !== 'undefined'
      ? `${window.location.origin}/qr-login/${objState.challengeID}`
      : '';

  return (
    <div className="space-y-4 text-center">
      <div className="relative mx-auto flex size-52 items-center justify-center rounded-2xl bg-white p-3">
        {objState.kind === 'ready' && (
          <QRCodeSVG
            value={strQrValue}
            size={184}
            level="M"
            title="Mã QR đăng nhập"
            className={objState.scanned ? 'opacity-15 blur-[2px]' : ''}
          />
        )}
        {objState.kind === 'loading' && (
          <LoaderCircle aria-label="Đang tạo mã" className="size-8 animate-spin text-slate-400" />
        )}
        {objState.kind === 'approved' && <CircleCheck aria-hidden className="size-14 text-emerald-500" />}
        {objState.kind === 'ready' && objState.scanned && (
          <div className="absolute inset-0 flex flex-col items-center justify-center gap-2 text-slate-800">
            <Smartphone aria-hidden className="size-9" />
            <p className="px-4 text-sm font-medium">Đã quét — xác nhận trên điện thoại</p>
          </div>
        )}
        {(objState.kind === 'expired' || objState.kind === 'error') && (
          <button
            type="button"
            onClick={handleRefresh}
            className="flex flex-col items-center gap-2 text-sm font-medium text-slate-800 hover:text-slate-950"
          >
            <RefreshCw aria-hidden className="size-8" />
            Tạo mã mới
          </button>
        )}
      </div>
      <div aria-live="polite" className="min-h-10 text-sm">
        {objState.kind === 'ready' && (
          <p className="text-slate-300">
            Mở camera trên điện thoại <span className="text-slate-400">(đã đăng nhập HPM Shop)</span> và quét mã.
            <br />
            <span className="text-xs text-slate-500">Mã hết hạn sau {intSecondsLeft}s</span>
          </p>
        )}
        {objState.kind === 'approved' && <p className="text-emerald-300">Đăng nhập thành công, đang chuyển trang…</p>}
        {(objState.kind === 'expired' || objState.kind === 'error') && (
          <p className="text-amber-200">{objState.message}</p>
        )}
      </div>
    </div>
  );
}
