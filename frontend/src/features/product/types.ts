export type CategorySlug =
  'ban-phim' | 'chuot' | 'tai-nghe' | 'loa' | 'sac-cap' | 'pin-du-phong' | 'phu-kien-ban' | 'dong-ho';

export type Category = { slug: CategorySlug; name: string; tagline: string };

export type ProductSpec = { label: string; value: string };

export type Product = {
  id: string;
  slug: string;
  name: string;
  brand: string;
  categorySlug: CategorySlug;
  price: number;
  oldPrice?: number;
  rating: number;
  reviewCount: number;
  soldCount: number;
  badge?: 'Mới' | 'Bán chạy' | 'Giảm sốc';
  colors: string[];
  summary: string;
  specs: ProductSpec[];
  /** 2 màu nền cho ảnh minh họa tạm (chưa có ảnh thật) */
  tone: [string, string];
};

export type ProductSort = 'popular' | 'newest' | 'price-asc' | 'price-desc';

export type ProductQuery = {
  category?: CategorySlug;
  keyword?: string;
  sort?: ProductSort;
  maxPrice?: number;
  page?: number;
  pageSize?: number;
};

export type ProductPage = { items: Product[]; total: number; page: number; pageCount: number };
