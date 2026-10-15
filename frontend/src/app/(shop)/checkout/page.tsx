import type { Metadata } from 'next';

import { PagePlaceholder } from '@/components/layout/page-placeholder';

export const metadata: Metadata = { title: 'Thanh toán' };

export default function CheckoutPage() {
  return (
    <PagePlaceholder title="Thanh toán">
      Bước thanh toán sẽ có khi ApiCore8 có API đơn hàng. Giỏ hàng của bạn vẫn được giữ nguyên.
    </PagePlaceholder>
  );
}
