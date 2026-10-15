'use client';

import { usePathname } from 'next/navigation';
import { useLayoutEffect, useState } from 'react';
import type { ReactNode } from 'react';

import { cn } from '@/lib/utils';

import { notifyAuthRouteChanged } from '../dust-transition';
import { INITIAL_CORD_PATH, MAX_PULL, ROPE_LENGTH, usePullCord } from '../hooks/use-pull-cord';

type LampSceneProps = { children: ReactNode };

// Khung SVG chứa dây: đủ rộng cho dây đung đưa, điểm treo ở giữa cạnh trên
const CORD_BOX_WIDTH = 220;

/** Khung trang xác thực: phòng tối, đèn treo — kéo dây (hoặc bấm/Enter) bật đèn thì form mới hiện ra */
export function LampScene({ children }: LampSceneProps) {
  const [bolLightOn, setBolLightOn] = useState(false);
  const { objSvgRef, objPathRef, objHitPathRef, objKnobRef, bolDragging, handleBrush, cordHandlers, handleKnobClick } =
    usePullCord({
      onToggle: () => setBolLightOn((x) => !x),
    });

  const strPathname = usePathname();

  // Layout effect: chạy trước khi trình duyệt vẽ route mới → kịp ẩn form mới cho bụi sáng tụ lại
  useLayoutEffect(() => notifyAuthRouteChanged(), [strPathname]);

  return (
    <div
      onPointerMove={handleBrush}
      className={cn(
        'relative flex min-h-dvh flex-1 flex-col items-center overflow-hidden px-4 pb-3 text-slate-100 transition-colors duration-700 motion-reduce:transition-none tall:pb-8',
        bolLightOn ? 'bg-night' : 'bg-[#020308]',
      )}
    >
      <div className="relative z-20 flex flex-col items-center">
        {/* Dây điện treo trần */}
        <div aria-hidden className={cn('h-2 w-px sm:h-6 tall:h-20', bolLightOn ? 'bg-slate-800' : 'bg-white/[0.03]')} />

        <div className="relative">
          {/* Chao đèn vòm kim loại */}
          <div
            aria-hidden
            className={cn(
              'relative h-21 w-42 rounded-t-full transition-[background] duration-700',
              bolLightOn
                ? 'bg-[radial-gradient(120%_90%_at_35%_15%,#4a5468_0%,#232a3a_55%,#141927_100%)]'
                : 'bg-[radial-gradient(120%_90%_at_35%_15%,#07090f_0%,#04050a_70%)]',
            )}
          >
            <span className="absolute top-2 left-1/2 h-1.5 w-6 -translate-x-1/2 rounded-full bg-black/40" />
          </div>
          {/* Miệng chao: lòng đèn hắt sáng khi bật */}
          <div
            aria-hidden
            className={cn(
              'absolute -bottom-1.5 left-0 h-3 w-full rounded-[50%] transition-colors duration-500',
              bolLightOn ? 'animate-lamp-flicker bg-amber-100 motion-reduce:animate-none' : 'bg-[#030409]',
            )}
          />
          {/* Bóng đèn ló dưới chao */}
          <div
            data-lamp-bulb
            aria-hidden
            className={cn(
              'absolute top-full left-1/2 size-8 -translate-x-1/2 -translate-y-1/2 rounded-full transition-colors duration-500',
              bolLightOn
                ? 'animate-lamp-flicker bg-amber-50 shadow-[0_0_30px_10px_rgba(255,226,150,0.65),0_0_120px_40px_rgba(255,214,120,0.25)] motion-reduce:animate-none'
                : 'bg-[#05060b]',
            )}
          />

          {/* Quầng sáng hình nón, viền mờ (blur bọc ngoài clip-path) */}
          <div
            aria-hidden
            className={cn(
              'pointer-events-none absolute top-full left-1/2 -z-10 -translate-x-1/2 blur-2xl',
              bolLightOn ? 'animate-lamp-flicker motion-reduce:animate-none' : 'opacity-0',
            )}
          >
            <div className="h-[115dvh] w-[min(980px,170vw)] bg-[radial-gradient(60%_85%_at_50%_0%,rgba(255,221,150,0.34)_0%,rgba(255,214,130,0.12)_45%,transparent_78%)] [clip-path:polygon(42%_0,58%_0,100%_100%,0_100%)]" />
          </div>

          {/* Dây kéo công tắc: chuỗi hạt (nét đứt bo tròn) + quả nắm bằng đồng, mô phỏng vật lý trong usePullCord */}
          <div
            className="pointer-events-none absolute top-[calc(100%-4px)]"
            style={{ right: 40 - CORD_BOX_WIDTH / 2, width: CORD_BOX_WIDTH, height: ROPE_LENGTH + MAX_PULL + 24 }}
          >
            <svg ref={objSvgRef} aria-hidden className="absolute inset-0 size-full touch-none overflow-visible">
              <g transform={`translate(${CORD_BOX_WIDTH / 2} 0)`}>
                <path
                  ref={objPathRef}
                  d={INITIAL_CORD_PATH}
                  fill="none"
                  stroke="currentColor"
                  strokeWidth={2.6}
                  strokeLinecap="round"
                  strokeDasharray="0.1 3.6"
                  className="text-slate-500"
                />
                {/* Vùng nắm vô hình rộng 20px dọc theo dây */}
                <path
                  ref={objHitPathRef}
                  d={INITIAL_CORD_PATH}
                  fill="none"
                  stroke="transparent"
                  strokeWidth={20}
                  strokeLinecap="round"
                  {...cordHandlers}
                  className={cn('pointer-events-auto', bolDragging ? 'cursor-grabbing' : 'cursor-grab')}
                />
              </g>
            </svg>
            <button
              ref={objKnobRef}
              type="button"
              {...cordHandlers}
              onClick={handleKnobClick}
              aria-pressed={bolLightOn}
              aria-label={bolLightOn ? 'Tắt đèn' : 'Kéo dây bật đèn'}
              className={cn(
                'group pointer-events-auto absolute top-0 left-1/2 flex size-10 touch-none items-center justify-center outline-none select-none',
                bolDragging ? 'cursor-grabbing' : 'cursor-grab',
              )}
              style={{ transform: `translate(-50%, calc(${ROPE_LENGTH}px - 50%))` }}
            >
              <span
                className={cn(
                  'size-3.5 rounded-full bg-[radial-gradient(circle_at_35%_30%,#f6d58b,#a8742c_70%)] group-focus-visible:ring-2 group-focus-visible:ring-amber-200 group-focus-visible:ring-offset-2 group-focus-visible:ring-offset-black',
                  // Lúc tối chỉ còn ánh le lói của quả nắm để người dùng tìm thấy dây
                  bolLightOn ? '' : 'shadow-[0_0_10px_2px_rgba(246,213,139,0.35)]',
                )}
              />
            </button>
          </div>
        </div>
      </div>

      <div
        data-auth-stage
        inert={!bolLightOn}
        className={cn(
          'relative z-10 mt-16 w-full max-w-sm transition-[opacity,translate] duration-700 has-[[data-wide]]:max-w-xl motion-reduce:transition-none tall:mt-24',
          bolLightOn ? 'translate-y-0 opacity-100 delay-300' : 'pointer-events-none translate-y-2 opacity-0',
        )}
      >
        {children}
      </div>
    </div>
  );
}
