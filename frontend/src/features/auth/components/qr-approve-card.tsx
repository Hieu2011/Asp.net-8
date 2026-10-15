'use client';

import Link from 'next/link';
import { useState, useTransition } from 'react';

import { CircleCheck, CircleX, Monitor } from 'lucide-react';

import { describeDevice } from '@/lib/device';
import { formatDateTime } from '@/lib/format';

import { describeAuthError } from '../error-message';
import { decideQrLoginAction } from '../qr-actions';
import type { QrChallengeInfo } from '../types';

type QrApproveCardProps = { challengeID: string; info: QrChallengeInfo; accountLabel: string };
type Decision = 'approved' | 'rejected' | null;

/** Điện thoại xác nhận cho máy tính đăng nhập — hiện rõ thiết bị/IP để người dùng nhận ra yêu cầu lạ */
export function QrApproveCard({ challengeID, info, accountLabel }: QrApproveCardProps) {
  const [strDecision, setStrDecision] = useState<Decision>(null);
  const [strError, setStrError] = useState('');
  const [bolPending, startTransition] = useTransition();

  const handleDecide = (bolApprove: boolean) =>
    startTransition(async () => {
      const objResult = await decideQrLoginAction(challengeID, bolApprove);
      if (!objResult.ok) return setStrError(describeAuthError(objResult));
      setStrDecision(bolApprove ? 'approved' : 'rejected');
    });

  if (strDecision) {
    const bolApproved = strDecision === 'approved';
    return (
      <div className="space-y-4 text-center">
        {bolApproved ? (
          <CircleCheck aria-hidden className="mx-auto size-14 text-emerald-400" />
        ) : (
          <CircleX aria-hidden className="mx-auto size-14 text-rose-400" />
        )}
        <h1 className="text-xl font-semibold text-slate-50">{bolApproved ? 'Đã cho phép đăng nhập' : 'Đã từ chối'}</h1>
        <p className="text-sm text-slate-400">
          {bolApproved
            ? 'Máy tính sẽ tự vào tài khoản trong giây lát.'
            : 'Máy tính kia không đăng nhập được bằng mã này.'}
        </p>
        <Link href="/" className="inline-block text-sm font-medium text-amber-200 hover:text-amber-100">
          Về trang chủ
        </Link>
      </div>
    );
  }

  return (
    <div className="space-y-5">
      <div className="text-center">
        <h1 className="text-xl font-semibold text-slate-50">Đăng nhập trên thiết bị khác?</h1>
        <p className="mt-1 text-sm break-all text-slate-400">
          Tài khoản <span className="text-amber-100">{accountLabel}</span>
        </p>
      </div>
      <div className="flex items-center gap-3 rounded-xl bg-white/5 p-4 ring-1 ring-white/10">
        <Monitor aria-hidden className="size-8 shrink-0 text-amber-200" />
        <div className="min-w-0 text-sm">
          <p className="font-medium text-slate-100">{describeDevice(info.deviceInfo)}</p>
          <p className="text-slate-400">IP {info.ipAddress || 'không rõ'}</p>
          <p className="text-slate-500">Lúc {formatDateTime(info.createdAt)}</p>
        </div>
      </div>
      <p className="text-xs text-slate-400">
        Chỉ đồng ý nếu chính bạn vừa mở trang đăng nhập trên máy đó. Không quét mã do người lạ gửi.
      </p>
      {strError && (
        <p role="alert" className="text-sm text-rose-300">
          {strError}
        </p>
      )}
      <div className="grid grid-cols-2 gap-3">
        <button
          type="button"
          disabled={bolPending}
          onClick={() => handleDecide(false)}
          className="h-11 rounded-lg text-sm font-medium text-slate-200 ring-1 ring-white/15 hover:bg-white/5 disabled:opacity-50"
        >
          Từ chối
        </button>
        <button
          type="button"
          disabled={bolPending}
          onClick={() => handleDecide(true)}
          className="h-11 rounded-lg bg-amber-200 text-sm font-semibold text-slate-900 hover:bg-amber-100 disabled:opacity-50"
        >
          {bolPending ? 'Đang xử lý…' : 'Đồng ý'}
        </button>
      </div>
    </div>
  );
}
