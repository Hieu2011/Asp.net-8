import Link from 'next/link';

import { Logo } from './logo';

const FOOTER_GROUPS = [
  {
    title: 'Mua sắm',
    links: [
      { href: '/products', label: 'Tất cả sản phẩm' },
      { href: '/products?sort=newest', label: 'Hàng mới về' },
      { href: '/products?sort=price-asc', label: 'Giá tốt' },
    ],
  },
  {
    title: 'Hỗ trợ',
    links: [
      { href: '/support/shipping', label: 'Giao hàng & lắp đặt' },
      { href: '/support/warranty', label: 'Bảo hành 12 tháng' },
      { href: '/support/returns', label: 'Đổi trả trong 30 ngày' },
    ],
  },
  {
    title: 'Tài khoản',
    links: [
      { href: '/account', label: 'Thông tin tài khoản' },
      { href: '/orders', label: 'Đơn hàng của tôi' },
      { href: '/cart', label: 'Giỏ hàng' },
    ],
  },
];

export function SiteFooter() {
  return (
    <footer className="mt-16 bg-night text-slate-300">
      <div className="mx-auto grid max-w-7xl gap-10 px-4 py-12 sm:grid-cols-2 lg:grid-cols-[1.4fr_repeat(3,1fr)]">
        <div className="space-y-3 text-slate-100">
          <Logo />
          <p className="max-w-xs text-sm text-slate-400">
            Phụ kiện công nghệ cho góc làm việc gọn gàng — chọn kỹ, bảo hành rõ ràng, giao nhanh toàn quốc.
          </p>
          <p className="text-sm text-slate-400">Hotline: 1900 0000 (8:00 – 21:00)</p>
        </div>
        {FOOTER_GROUPS.map((x) => (
          <nav key={x.title} aria-label={x.title}>
            <h2 className="text-sm font-semibold text-slate-100">{x.title}</h2>
            <ul className="mt-3 space-y-2 text-sm">
              {x.links.map((y) => (
                <li key={y.href}>
                  <Link href={y.href} className="hover:text-lamp-soft">
                    {y.label}
                  </Link>
                </li>
              ))}
            </ul>
          </nav>
        ))}
      </div>
      <div className="border-t border-white/5">
        <p className="mx-auto max-w-7xl px-4 py-4 text-xs text-slate-500">
          © {new Date().getFullYear()} HPM Shop. Dự án cá nhân, dữ liệu sản phẩm là dữ liệu mẫu.
        </p>
      </div>
    </footer>
  );
}
