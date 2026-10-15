import type { NextConfig } from 'next';

const nextConfig: NextConfig = {
  // Build ra thư mục tự chạy được (server.js + node_modules tối thiểu) → image Docker nhỏ
  output: 'standalone',
  reactCompiler: true,
  poweredByHeader: false,
};

export default nextConfig;
