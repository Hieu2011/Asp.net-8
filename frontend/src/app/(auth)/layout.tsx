import { LampScene } from '@/features/auth/components/lamp-scene';

export default function AuthLayout({ children }: LayoutProps<'/'>) {
  return <LampScene>{children}</LampScene>;
}
