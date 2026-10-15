import { z } from 'zod';

// NEXT_PUBLIC_* được Next nhúng lúc build → phải gọi process.env.NEXT_PUBLIC_X tường minh từng biến
const objClientEnvSchema = z.object({
  NEXT_PUBLIC_SITE_URL: z.url().default('http://localhost:3000'),
  NEXT_PUBLIC_TURNSTILE_SITE_KEY: z.string().default(''),
  NEXT_PUBLIC_GOOGLE_CLIENT_ID: z.string().default(''),
});

export const clientEnv = objClientEnvSchema.parse({
  NEXT_PUBLIC_SITE_URL: process.env.NEXT_PUBLIC_SITE_URL,
  NEXT_PUBLIC_TURNSTILE_SITE_KEY: process.env.NEXT_PUBLIC_TURNSTILE_SITE_KEY,
  NEXT_PUBLIC_GOOGLE_CLIENT_ID: process.env.NEXT_PUBLIC_GOOGLE_CLIENT_ID,
});
