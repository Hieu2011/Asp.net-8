'use client';

import { useState } from 'react';

import { Check } from 'lucide-react';

import { Button } from '@/components/ui/button';
import type { Product } from '@/features/product/types';
import { cn } from '@/lib/utils';

import { MAX_QUANTITY, useCartStore } from '../store';
import { QuantityStepper } from './quantity-stepper';

type AddToCartProps = { product: Product };

export function AddToCart({ product }: AddToCartProps) {
  const addItem = useCartStore((x) => x.addItem);
  const [strColor, setStrColor] = useState(product.colors[0] ?? '');
  const [intQuantity, setIntQuantity] = useState(1);
  const [bolAdded, setBolAdded] = useState(false);

  const handleSelectColor = (strValue: string) => {
    setStrColor(strValue);
    setBolAdded(false);
  };

  const handleAdd = () => {
    const { id, slug, name, price, categorySlug, tone } = product;
    addItem({ productId: id, slug, name, price, categorySlug, tone, color: strColor }, intQuantity);
    setBolAdded(true);
  };

  return (
    <div className="space-y-5">
      <fieldset>
        <legend className="mb-2 text-sm font-medium text-ink">
          Màu: <span className="font-normal text-ink-muted">{strColor}</span>
        </legend>
        <div className="flex flex-wrap gap-2">
          {product.colors.map((x) => (
            <button
              key={x}
              type="button"
              aria-pressed={x === strColor}
              onClick={() => handleSelectColor(x)}
              className={cn(
                'rounded-full border px-3 py-1.5 text-sm transition-colors',
                x === strColor ? 'border-ink bg-ink text-white' : 'border-line bg-white text-ink hover:border-ink/40',
              )}
            >
              {x}
            </button>
          ))}
        </div>
      </fieldset>
      <div className="flex items-center gap-3">
        <QuantityStepper
          value={intQuantity}
          onChange={(intValue) => setIntQuantity(Math.min(MAX_QUANTITY, Math.max(1, intValue)))}
        />
        <Button
          type="button"
          onClick={handleAdd}
          className="h-11 flex-1 rounded-full bg-night text-base font-semibold text-lamp-soft hover:bg-night-soft"
        >
          {bolAdded ? (
            <>
              <Check className="size-4" /> Đã thêm vào giỏ
            </>
          ) : (
            'Thêm vào giỏ'
          )}
        </Button>
      </div>
    </div>
  );
}
