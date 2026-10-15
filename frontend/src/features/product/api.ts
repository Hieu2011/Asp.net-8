import 'server-only';

import { CATEGORIES, PRODUCTS } from './data';
import type { Category, CategorySlug, Product, ProductPage, ProductQuery } from './types';

const DEFAULT_PAGE_SIZE = 12;

// Truy cập dữ liệu sản phẩm — hiện đọc mock, sau thay bằng gọi ApiCore8 (giữ nguyên chữ ký)

export async function getCategories(): Promise<Category[]> {
  return CATEGORIES;
}

export async function getCategory(strSlug: string): Promise<Category | undefined> {
  return CATEGORIES.find((x) => x.slug === strSlug);
}

export async function getProducts(objQuery: ProductQuery = {}): Promise<ProductPage> {
  const { category, keyword, sort = 'popular', maxPrice, page = 1, pageSize = DEFAULT_PAGE_SIZE } = objQuery;
  const strKeyword = keyword?.trim().toLowerCase();
  const lstFiltered = PRODUCTS.filter(
    (x) =>
      (!category || x.categorySlug === category) &&
      (!maxPrice || x.price <= maxPrice) &&
      (!strKeyword || `${x.name} ${x.brand}`.toLowerCase().includes(strKeyword)),
  ).toSorted((a, b) => {
    if (sort === 'price-asc') return a.price - b.price;
    if (sort === 'price-desc') return b.price - a.price;
    if (sort === 'newest') return Number(b.badge === 'Mới') - Number(a.badge === 'Mới');
    return b.soldCount - a.soldCount;
  });
  const intPageCount = Math.max(1, Math.ceil(lstFiltered.length / pageSize));
  const intPage = Math.min(Math.max(1, page), intPageCount);
  return {
    items: lstFiltered.slice((intPage - 1) * pageSize, intPage * pageSize),
    total: lstFiltered.length,
    page: intPage,
    pageCount: intPageCount,
  };
}

export async function getProductBySlug(strSlug: string): Promise<Product | undefined> {
  return PRODUCTS.find((x) => x.slug === strSlug);
}

export async function getRelatedProducts(objProduct: Product, intLimit = 4): Promise<Product[]> {
  return PRODUCTS.filter((x) => x.categorySlug === objProduct.categorySlug && x.id !== objProduct.id)
    .concat(PRODUCTS.filter((x) => x.categorySlug !== objProduct.categorySlug))
    .slice(0, intLimit);
}

export async function getFlashSaleProducts(intLimit = 4): Promise<Product[]> {
  return PRODUCTS.filter((x) => x.oldPrice)
    .toSorted((a, b) => b.oldPrice! - b.price - (a.oldPrice! - a.price))
    .slice(0, intLimit);
}

export const isCategorySlug = (strValue: unknown): strValue is CategorySlug =>
  CATEGORIES.some((x) => x.slug === strValue);
