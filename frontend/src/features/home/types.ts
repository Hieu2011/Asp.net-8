import type { Product } from '@/features/product/types';

export type HeroSlide = {
  id: string;
  eyebrow: string;
  title: string;
  description: string;
  /** Màu nhấn của slide: quầng sáng nền, chip nổi */
  accent: string;
  /** Chip nổi trên sản phẩm: "−23%", "Top 1"... */
  highlight: string;
  product: Product;
  secondaryLink: { label: string; href: string };
};
