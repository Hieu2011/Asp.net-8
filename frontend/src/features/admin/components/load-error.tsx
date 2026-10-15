import { TriangleAlert } from 'lucide-react';

type LoadErrorProps = { message?: string };

export function LoadError({ message = 'Không tải được dữ liệu từ máy chủ, vui lòng thử lại sau.' }: LoadErrorProps) {
  return (
    <div
      role="alert"
      className="flex items-center gap-3 rounded-2xl bg-rose-50 p-4 text-sm text-rose-700 ring-1 ring-rose-200"
    >
      <TriangleAlert aria-hidden className="size-5 shrink-0" />
      {message}
    </div>
  );
}
