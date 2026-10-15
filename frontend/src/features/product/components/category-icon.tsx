import { BatteryCharging, Cable, Headphones, Keyboard, Lamp, Mouse, Speaker, Watch } from 'lucide-react';
import type { LucideProps, Gamepad2 } from 'lucide-react';

import type { CategorySlug } from '../types';

const ICON_BY_CATEGORY = {
  'ban-phim': Keyboard,
  chuot: Mouse,
  'tai-nghe': Headphones,
  loa: Speaker,
  'sac-cap': Cable,
  'pin-du-phong': BatteryCharging,
  'phu-kien-ban': Lamp,
  'dong-ho': Watch,
} satisfies Record<CategorySlug, typeof Gamepad2>;

type CategoryIconProps = LucideProps & { slug: CategorySlug };

export function CategoryIcon({ slug, ...props }: CategoryIconProps) {
  const Icon = ICON_BY_CATEGORY[slug];
  return <Icon aria-hidden {...props} />;
}
