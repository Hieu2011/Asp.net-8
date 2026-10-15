import type { CategorySlug, ProductSort } from './types';

export const SORT_OPTIONS: { value: ProductSort; label: string }[] = [
  { value: 'popular', label: 'Bán chạy' },
  { value: 'newest', label: 'Mới nhất' },
  { value: 'price-asc', label: 'Giá thấp → cao' },
  { value: 'price-desc', label: 'Giá cao → thấp' },
];

export const PRICE_OPTIONS = [
  { value: 500_000, label: 'Dưới 500.000đ' },
  { value: 1_000_000, label: 'Dưới 1 triệu' },
  { value: 2_000_000, label: 'Dưới 2 triệu' },
];

export type ProductFilterState = { category?: CategorySlug; keyword?: string; sort: ProductSort; maxPrice?: number };

/** Dựng URL danh sách giữ các bộ lọc hiện tại, ghi đè phần thay đổi (page về 1) */
export function buildProductsHref(
  objState: ProductFilterState,
  objChange: Partial<ProductFilterState> & { page?: number },
) {
  const objMerged = { ...objState, ...objChange };
  const objParams = new URLSearchParams();
  if (objMerged.category) objParams.set('category', objMerged.category);
  if (objMerged.keyword) objParams.set('q', objMerged.keyword);
  if (objMerged.sort !== 'popular') objParams.set('sort', objMerged.sort);
  if (objMerged.maxPrice) objParams.set('maxPrice', String(objMerged.maxPrice));
  if (objChange.page && objChange.page > 1) objParams.set('page', String(objChange.page));
  const strQuery = objParams.toString();
  return strQuery ? `/products?${strQuery}` : '/products';
}

export const isProductSort = (strValue: unknown): strValue is ProductSort =>
  SORT_OPTIONS.some((x) => x.value === strValue);
