import { create } from 'zustand';
import { createJSONStorage, persist } from 'zustand/middleware';

import type { CategorySlug } from '@/features/product/types';

export type CartItem = {
  key: string;
  productId: string;
  slug: string;
  name: string;
  price: number;
  color: string;
  quantity: number;
  categorySlug: CategorySlug;
  tone: [string, string];
};

type CartState = {
  lstItem: CartItem[];
  addItem: (objItem: Omit<CartItem, 'key' | 'quantity'>, intQuantity?: number) => void;
  setQuantity: (strKey: string, intQuantity: number) => void;
  removeItem: (strKey: string) => void;
  clear: () => void;
};

export const MAX_QUANTITY = 20;

// Giỏ hàng lưu localStorage (chỉ thông tin sản phẩm, không có dữ liệu nhạy cảm)
export const useCartStore = create<CartState>()(
  persist(
    (set) => ({
      lstItem: [],
      addItem: (objItem, intQuantity = 1) =>
        set((objState) => {
          const strKey = `${objItem.productId}:${objItem.color}`;
          const objExisting = objState.lstItem.find((x) => x.key === strKey);
          if (!objExisting)
            return { lstItem: [...objState.lstItem, { ...objItem, key: strKey, quantity: intQuantity }] };
          return {
            lstItem: objState.lstItem.map((x) =>
              x.key === strKey ? { ...x, quantity: Math.min(MAX_QUANTITY, x.quantity + intQuantity) } : x,
            ),
          };
        }),
      setQuantity: (strKey, intQuantity) =>
        set((objState) => ({
          lstItem: objState.lstItem.map((x) =>
            x.key === strKey ? { ...x, quantity: Math.min(MAX_QUANTITY, Math.max(1, intQuantity)) } : x,
          ),
        })),
      removeItem: (strKey) => set((objState) => ({ lstItem: objState.lstItem.filter((x) => x.key !== strKey) })),
      clear: () => set({ lstItem: [] }),
    }),
    { name: 'hpm-cart', storage: createJSONStorage(() => localStorage) },
  ),
);

export const selectCartCount = (objState: CartState) => objState.lstItem.reduce((intSum, x) => intSum + x.quantity, 0);

export const selectCartTotal = (objState: CartState) =>
  objState.lstItem.reduce((decSum, x) => decSum + x.price * x.quantity, 0);
