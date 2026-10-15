import 'server-only';
import { z } from 'zod';

const objServerEnvSchema = z.object({
  AUTH_API_URL: z.url(),
});

let objServerEnv: z.infer<typeof objServerEnvSchema> | undefined;

// Đọc lúc chạy chứ không lúc nạp module — next build nạp module khi chưa có env runtime (Docker/CI)
export function getServerEnv() {
  objServerEnv ??= objServerEnvSchema.parse({ AUTH_API_URL: process.env.AUTH_API_URL });
  return objServerEnv;
}
