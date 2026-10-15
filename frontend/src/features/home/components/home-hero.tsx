import type { Product } from '@/features/product/types';
import { calcDiscountPercent } from '@/lib/format';

import { HeroCarousel } from './hero-carousel';
import type { HeroSlide } from '../types';

type HomeHeroProps = { bestSeller?: Product; newest?: Product; onSale?: Product };

/** Banner trang chủ: dựng nội dung slide phía server, phần 3D/tương tác giao cho HeroCarousel */
export function HomeHero({ bestSeller, newest, onSale }: HomeHeroProps) {
  const lstSlide = [
    bestSeller && {
      id: 'best-seller',
      eyebrow: 'Bán chạy nhất tuần',
      title: 'Bàn làm việc đẹp hơn sau mỗi lần nâng cấp nhỏ',
      description: `${bestSeller.name} — món được dân văn phòng chọn nhiều nhất, gõ êm và bền bỉ cả ngày dài.`,
      accent: '#f5c451',
      highlight: 'Top 1',
      product: bestSeller,
      secondaryLink: { label: 'Xem bán chạy', href: '/products?sort=popular' },
    },
    newest && {
      id: 'newest',
      eyebrow: 'Hàng mới về',
      title: 'Đồ mới lên kệ, thử trước khi ai cũng có',
      description: `${newest.name} vừa cập bến HPM Shop — giao nhanh 2 giờ, dùng thử 7 ngày tại nhà.`,
      accent: '#5eead4',
      highlight: 'Mới',
      product: newest,
      secondaryLink: { label: 'Xem hàng mới', href: '/products?sort=newest' },
    },
    onSale && {
      id: 'on-sale',
      eyebrow: 'Giá tốt hôm nay',
      title: 'Giảm sâu cho góc máy thêm chất',
      description: `${onSale.name} đang giảm giá có hạn — số lượng ít, hết là về giá cũ.`,
      accent: '#fb7185',
      highlight: `−${calcDiscountPercent(onSale.price, onSale.oldPrice)}%`,
      product: onSale,
      secondaryLink: { label: 'Săn deal khác', href: '/products?sort=price-asc' },
    },
  ].filter((x): x is HeroSlide => Boolean(x));

  if (lstSlide.length === 0) return null;
  return <HeroCarousel slides={lstSlide} />;
}
