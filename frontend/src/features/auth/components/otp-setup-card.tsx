import Link from 'next/link';

import { QRCodeSVG } from 'qrcode.react';

import { Button } from '@/components/ui/button';

// onContinue: dùng khi thẻ nằm ngay trên /login — Link tới chính URL hiện tại thì Next không làm gì
type OtpSetupCardProps = { otpauthUri: string; title: string; onContinue?: () => void };

/** Hướng dẫn thêm tài khoản vào Google Authenticator từ otpauth:// URI */
export function OtpSetupCard({ otpauthUri, title, onContinue }: OtpSetupCardProps) {
  const strSecret = new URL(otpauthUri).searchParams.get('secret') ?? '';
  // Tách 4 ký tự/nhóm cho dễ nhập tay
  const strSecretDisplay = strSecret.replace(/(.{4})/g, '$1 ').trim();

  return (
    <div className="space-y-5 text-center">
      <h1 className="text-2xl font-semibold text-slate-50">{title}</h1>
      <ol className="space-y-2 text-left text-sm text-slate-300">
        <li>1. Mở Google Authenticator (hoặc ứng dụng OTP tương tự).</li>
        <li>2. Quét mã QR — hoặc chọn “Nhập khóa thiết lập” và nhập khóa bên dưới.</li>
        <li>3. Dùng mã 6 số trong ứng dụng để đăng nhập.</li>
      </ol>
      {/* QR sinh ngay trên trình duyệt — không gửi secret cho dịch vụ bên ngoài */}
      <div className="mx-auto w-fit rounded-xl bg-white p-3">
        <QRCodeSVG value={otpauthUri} size={176} level="M" title="Mã QR thiết lập OTP" />
      </div>
      <p className="rounded-lg border border-amber-200/30 bg-slate-900/60 px-3 py-3 font-mono text-lg tracking-wider text-amber-100 select-all">
        {strSecretDisplay}
      </p>
      <div className="flex flex-col gap-2">
        <Button render={<a href={otpauthUri} />} nativeButton={false} variant="outline" className="h-10">
          Mở ứng dụng xác thực
        </Button>
        {onContinue ? (
          <Button type="button" onClick={onContinue} className="h-10 bg-amber-200 text-slate-900 hover:bg-amber-100">
            Đăng nhập ngay
          </Button>
        ) : (
          <Button
            render={<Link href="/login" />}
            nativeButton={false}
            className="h-10 bg-amber-200 text-slate-900 hover:bg-amber-100"
          >
            Đến trang đăng nhập
          </Button>
        )}
      </div>
      <p className="text-xs text-slate-500">Khóa chỉ hiển thị 1 lần — lưu lại trước khi rời trang.</p>
    </div>
  );
}
