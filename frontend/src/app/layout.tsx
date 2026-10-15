import type { Metadata } from 'next';
import { Be_Vietnam_Pro } from 'next/font/google';

import { Providers } from './providers';

import './globals.css';

const fontSans = Be_Vietnam_Pro({
  variable: '--font-sans',
  subsets: ['latin', 'vietnamese'],
  weight: ['400', '500', '600', '700'],
});

export const metadata: Metadata = {
  title: { default: 'HPM Shop', template: '%s | HPM Shop' },
  description: 'Mua sắm trực tuyến',
};

export default function RootLayout({ children }: LayoutProps<'/'>) {
  return (
    <html lang="vi" className={`${fontSans.variable} h-full antialiased`} suppressHydrationWarning>
      <body className="flex min-h-full flex-col">
        <Providers>{children}</Providers>
      </body>
    </html>
  );
}
