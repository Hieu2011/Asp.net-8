'use client';

import Link from 'next/link';
import { useRouter } from 'next/navigation';
import type { ComponentProps, MouseEvent } from 'react';

import { navigateWithDust } from '../dust-transition';

type AuthSwitchLinkProps = Omit<ComponentProps<typeof Link>, 'href'> & { href: string };

/** Link giữa các trang đăng nhập/đăng ký/quên mật khẩu — chuyển trang kèm hiệu ứng form tan thành bụi sáng rồi tụ lại */
export function AuthSwitchLink({ href, onClick, ...props }: AuthSwitchLinkProps) {
  const objRouter = useRouter();

  const handleClick = (objEvent: MouseEvent<HTMLAnchorElement>) => {
    onClick?.(objEvent);
    // Mở tab mới (Ctrl/Cmd/Shift/chuột giữa) thì để trình duyệt tự xử lý
    if (objEvent.defaultPrevented || objEvent.button !== 0 || objEvent.metaKey || objEvent.ctrlKey || objEvent.shiftKey)
      return;
    objEvent.preventDefault();
    void navigateWithDust(() => objRouter.push(href));
  };

  return <Link href={href} onClick={handleClick} {...props} />;
}
