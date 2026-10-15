import Link from 'next/link';

import { CategoryStrip } from '@/features/home/components/category-strip';
import { FlashSaleCountdown } from '@/features/home/components/flash-sale-countdown';
import { HomeHero } from '@/features/home/components/home-hero';
import { getCategories, getFlashSaleProducts, getProducts } from '@/features/product/api';
import { ProductGrid } from '@/features/product/components/product-grid';

const PROMISES = [
  { title: 'Giao nhanh 2 giờ', detail: 'Nội thành Hà Nội và TP.HCM' },
  { title: 'Bảo hành 12 tháng', detail: '1 đổi 1 trong 30 ngày đầu' },
  { title: 'Dùng thử tại nhà', detail: 'Không hợp, trả lại trong 7 ngày' },
];

export default async function HomePage() {
  const [lstCategory, lstFlashSale, objBestSeller, objNewest] = await Promise.all([
    getCategories(),
    getFlashSaleProducts(4),
    getProducts({ sort: 'popular', pageSize: 8 }),
    getProducts({ sort: 'newest', pageSize: 1 }),
  ]);

  return (
    <>
      <HomeHero bestSeller={objBestSeller.items[0]} newest={objNewest.items[0]} onSale={lstFlashSale[0]} />

      <section aria-label="Cam kết" className="border-b border-line bg-white">
        <ul className="mx-auto grid max-w-7xl divide-line px-4 sm:grid-cols-3 sm:divide-x">
          {PROMISES.map((x) => (
            <li key={x.title} className="py-4 sm:px-6 sm:first:pl-0">
              <p className="font-semibold">{x.title}</p>
              <p className="text-sm text-ink-muted">{x.detail}</p>
            </li>
          ))}
        </ul>
      </section>

      <CategoryStrip categories={lstCategory} />

      <section aria-labelledby="flash-heading" className="mx-auto max-w-7xl px-4 pt-14">
        <div className="flex flex-wrap items-end justify-between gap-3">
          <div>
            <h2 id="flash-heading" className="text-2xl font-bold tracking-tight">
              Giá tốt hôm nay
            </h2>
            <FlashSaleCountdown />
          </div>
          <Link
            href="/products?sort=price-asc"
            className="text-sm font-medium text-ink underline-offset-4 hover:underline"
          >
            Xem tất cả
          </Link>
        </div>
        <div className="mt-5">
          <ProductGrid products={lstFlashSale} />
        </div>
      </section>

      <section aria-labelledby="best-heading" className="mx-auto max-w-7xl px-4 pt-14">
        <h2 id="best-heading" className="text-2xl font-bold tracking-tight">
          Được mua nhiều nhất
        </h2>
        <div className="mt-5">
          <ProductGrid products={objBestSeller.items} />
        </div>
      </section>
    </>
  );
}
