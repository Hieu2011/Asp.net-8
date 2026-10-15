import type { Metadata } from 'next';

import { PagePlaceholder } from '@/components/layout/page-placeholder';

export const metadata: Metadata = { title: 'Đơn hàng' };

export default function OrdersPage() {
  return <PagePlaceholder title="Đơn hàng của tôi">Bạn chưa có đơn hàng nào.</PagePlaceholder>;
}
