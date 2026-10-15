import type { Metadata } from 'next';
import { notFound } from 'next/navigation';

const SUPPORT_TOPICS = {
  shipping: {
    title: 'Giao hàng & lắp đặt',
    paragraphs: [
      'Giao nhanh trong 2 giờ tại nội thành Hà Nội và TP.HCM với đơn đặt trước 18:00.',
      'Các tỉnh thành khác giao trong 2–4 ngày. Miễn phí giao hàng cho đơn từ 500.000đ.',
    ],
  },
  warranty: {
    title: 'Bảo hành 12 tháng',
    paragraphs: [
      'Tất cả sản phẩm được bảo hành chính hãng 12 tháng.',
      'Lỗi do nhà sản xuất trong 30 ngày đầu được đổi sản phẩm mới cùng loại.',
    ],
  },
  returns: {
    title: 'Đổi trả trong 30 ngày',
    paragraphs: [
      'Đổi trả miễn phí trong 30 ngày nếu sản phẩm còn đầy đủ hộp và phụ kiện.',
      'Hoàn tiền về phương thức thanh toán ban đầu trong 3–5 ngày làm việc.',
    ],
  },
} as const;

type SupportTopic = keyof typeof SUPPORT_TOPICS;

const isSupportTopic = (strValue: string): strValue is SupportTopic => strValue in SUPPORT_TOPICS;

export function generateStaticParams() {
  return Object.keys(SUPPORT_TOPICS).map((x) => ({ topic: x }));
}

export async function generateMetadata({ params }: PageProps<'/support/[topic]'>): Promise<Metadata> {
  const { topic } = await params;
  return { title: isSupportTopic(topic) ? SUPPORT_TOPICS[topic].title : 'Hỗ trợ' };
}

export default async function SupportPage({ params }: PageProps<'/support/[topic]'>) {
  const { topic } = await params;
  if (!isSupportTopic(topic)) notFound();
  const objTopic = SUPPORT_TOPICS[topic];
  return (
    <article className="mx-auto max-w-2xl space-y-4 px-4 py-12">
      <h1 className="text-3xl font-bold tracking-tight">{objTopic.title}</h1>
      {objTopic.paragraphs.map((x) => (
        <p key={x} className="leading-relaxed text-ink-muted">
          {x}
        </p>
      ))}
    </article>
  );
}
