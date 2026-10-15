import 'server-only';
import { z } from 'zod';

// Env chỉ dùng phía server — sai/thiếu là báo lỗi ngay khi module được nạp
const objServerEnvSchema = z.object({
  AUTH_API_URL: z.url(),
});

export const serverEnv = objServerEnvSchema.parse({
  AUTH_API_URL: process.env.AUTH_API_URL,
});
