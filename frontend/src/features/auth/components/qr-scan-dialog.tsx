'use client';

import { useRouter } from 'next/navigation';
import { useEffect, useEffectEvent, useRef, useState } from 'react';

import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '@/components/ui/dialog';

type QrScanDialogProps = { open: boolean; onOpenChange: (bolOpen: boolean) => void };
type DecodeFrame = (objVideo: HTMLVideoElement) => Promise<string | null>;
type BarcodeDetectorLike = { detect: (objSource: HTMLVideoElement) => Promise<{ rawValue: string }[]> };
type BarcodeDetectorConstructor = {
  new (objOptions: { formats: string[] }): BarcodeDetectorLike;
  getSupportedFormats: () => Promise<string[]>;
};

const SCAN_INTERVAL_MS = 200;
const JSQR_MAX_SIDE = 640;
const QR_LOGIN_PATH = /^\/qr-login\/[\w-]{16,64}$/;

/** Popup mở camera sau trong trình duyệt, quét mã QR đăng nhập trên máy khác rồi chuyển tới trang xác nhận */
export function QrScanDialog({ open, onOpenChange }: QrScanDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Quét mã QR đăng nhập</DialogTitle>
          <DialogDescription>Hướng camera vào mã QR trên màn hình thiết bị cần đăng nhập.</DialogDescription>
        </DialogHeader>
        {/* Chỉ mount khi mở → đóng popup là tắt camera ngay, không chờ hiệu ứng đóng */}
        {open && <QrScanner onDone={() => onOpenChange(false)} />}
      </DialogContent>
    </Dialog>
  );
}

function QrScanner({ onDone }: { onDone: () => void }) {
  const objRouter = useRouter();
  const objVideoRef = useRef<HTMLVideoElement>(null);
  const [strError, setStrError] = useState('');
  const [strHint, setStrHint] = useState('');
  const [bolReady, setBolReady] = useState(false);

  // true = đã nhận mã đăng nhập hợp lệ → dừng quét
  const handleDecoded = useEffectEvent((strRaw: string) => {
    const strPath = toQrLoginPath(strRaw);
    if (!strPath) {
      setStrHint('Đây không phải mã QR đăng nhập của trang này');
      return false;
    }
    onDone();
    objRouter.push(strPath);
    return true;
  });

  useEffect(() => {
    let bolStopped = false;
    let objStream: MediaStream | null = null;
    let intTimer = 0;

    const handleFail = (error: unknown) => {
      if (bolStopped) return;
      console.error('Quét QR lỗi', error);
      setStrError('Không mở được camera. Vui lòng thử lại.');
    };
    const startAsync = async () => {
      if (!window.isSecureContext || !navigator.mediaDevices?.getUserMedia)
        return setStrError('Trình duyệt chỉ cho mở camera trên trang https. Hãy dùng ứng dụng camera để quét mã.');
      try {
        objStream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: 'environment' }, audio: false });
      } catch (error) {
        return setStrError(describeCameraError(error));
      }
      const objVideo = objVideoRef.current;
      if (bolStopped || !objVideo) return objStream.getTracks().forEach((x) => x.stop());
      objVideo.srcObject = objStream;
      await objVideo.play();
      const decodeFrame = await createDecoder();
      if (bolStopped) return;
      setBolReady(true);
      const scanAsync = async () => {
        if (bolStopped) return;
        const strRaw = await decodeFrame(objVideo);
        if (strRaw && handleDecoded(strRaw)) return;
        intTimer = window.setTimeout(() => scanAsync().catch(handleFail), SCAN_INTERVAL_MS);
      };
      await scanAsync();
    };

    startAsync().catch(handleFail);
    return () => {
      bolStopped = true;
      window.clearTimeout(intTimer);
      objStream?.getTracks().forEach((x) => x.stop());
    };
  }, []);

  if (strError) return <p className="rounded-lg bg-rose-500/10 p-3 text-sm text-rose-300">{strError}</p>;

  return (
    <div className="space-y-3">
      <div className="relative aspect-square overflow-hidden rounded-lg bg-black">
        <video ref={objVideoRef} playsInline muted className="size-full object-cover" />
        <div aria-hidden className="absolute inset-[15%] rounded-xl border-2 border-amber-200/80" />
        {!bolReady && (
          <p className="absolute inset-0 flex items-center justify-center text-sm text-slate-300">Đang mở camera…</p>
        )}
      </div>
      <p aria-live="polite" className="min-h-5 text-center text-xs text-muted-foreground">
        {strHint}
      </p>
    </div>
  );
}

// Chỉ nhận đúng link /qr-login/<mã> của chính trang này — không mở link lạ từ mã QR
function toQrLoginPath(strRaw: string): string | null {
  if (!URL.canParse(strRaw)) return null;
  const objUrl = new URL(strRaw);
  return objUrl.origin === window.location.origin && QR_LOGIN_PATH.test(objUrl.pathname) ? objUrl.pathname : null;
}

// Ưu tiên BarcodeDetector có sẵn (Android Chrome); không có (iPhone Safari) mới tải jsQR
async function createDecoder(): Promise<DecodeFrame> {
  const BarcodeDetector = (window as unknown as { BarcodeDetector?: BarcodeDetectorConstructor }).BarcodeDetector;
  if (BarcodeDetector && (await BarcodeDetector.getSupportedFormats()).includes('qr_code')) {
    const objDetector = new BarcodeDetector({ formats: ['qr_code'] });
    return async (objVideo) => (await objDetector.detect(objVideo))[0]?.rawValue ?? null;
  }
  const { default: jsQR } = await import('jsqr');
  const objCanvas = document.createElement('canvas');
  const objContext = objCanvas.getContext('2d', { willReadFrequently: true });
  return async (objVideo) => {
    if (!objContext || !objVideo.videoWidth) return null;
    // Thu nhỏ khung hình để giải mã nhanh trên máy yếu
    const decScale = Math.min(1, JSQR_MAX_SIDE / Math.max(objVideo.videoWidth, objVideo.videoHeight));
    const intWidth = Math.round(objVideo.videoWidth * decScale);
    const intHeight = Math.round(objVideo.videoHeight * decScale);
    objCanvas.width = intWidth;
    objCanvas.height = intHeight;
    objContext.drawImage(objVideo, 0, 0, intWidth, intHeight);
    const objImage = objContext.getImageData(0, 0, intWidth, intHeight);
    return jsQR(objImage.data, intWidth, intHeight, { inversionAttempts: 'dontInvert' })?.data ?? null;
  };
}

function describeCameraError(error: unknown): string {
  const strName = error instanceof DOMException ? error.name : '';
  if (strName === 'NotAllowedError')
    return 'Bạn đã chặn quyền camera. Hãy cho phép camera cho trang này trong cài đặt trình duyệt.';
  if (strName === 'NotFoundError' || strName === 'OverconstrainedError')
    return 'Không tìm thấy camera trên thiết bị này.';
  if (strName === 'NotReadableError') return 'Camera đang được ứng dụng khác sử dụng.';
  console.error('Mở camera lỗi', error);
  return 'Không mở được camera. Vui lòng thử lại.';
}
