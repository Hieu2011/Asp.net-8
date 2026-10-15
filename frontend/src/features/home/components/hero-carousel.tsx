'use client';

import Link from 'next/link';
import { useRef, useState } from 'react';
import type { FocusEvent, KeyboardEvent, PointerEvent } from 'react';

import { ChevronLeft, ChevronRight, Pause, Play } from 'lucide-react';

import { Button } from '@/components/ui/button';
import { PriceTag } from '@/features/product/components/price-tag';
import { ProductVisual } from '@/features/product/components/product-visual';
import { cn } from '@/lib/utils';

import type { HeroSlide } from '../types';

const MAX_TILT_DEG = 7;
const SWIPE_THRESHOLD = 50;

type HeroCarouselProps = { slides: HeroSlide[] };

// Vị trí thẻ so với slide đang hiện, quay vòng: -1 trái, 0 giữa, 1 phải, còn lại ẩn phía sau
const getOffset = (intIndex: number, intActive: number, intTotal: number) => {
  const intDiff = (intIndex - intActive + intTotal) % intTotal;
  return intDiff > intTotal / 2 ? intDiff - intTotal : intDiff;
};

const getCardTransform = (intOffset: number) =>
  `translate(-50%, -50%) translateX(${intOffset * 64}%) translateZ(${intOffset === 0 ? 0 : -260}px) rotateY(${intOffset * -38}deg)`;

/** Banner trình chiếu 3D (CSS): thẻ sản phẩm xếp vòng coverflow, cả sân khấu nghiêng theo chuột, sàn lưới phối cảnh */
export function HeroCarousel({ slides }: HeroCarouselProps) {
  const [intActive, setIntActive] = useState(0);
  const [bolHoverPaused, setBolHoverPaused] = useState(false);
  const [bolFocusPaused, setBolFocusPaused] = useState(false);
  const [bolUserPaused, setBolUserPaused] = useState(false);
  const objTiltRef = useRef<HTMLDivElement>(null);
  const objSwipeStartRef = useRef<number | null>(null);

  const intTotal = slides.length;
  const bolPaused = bolHoverPaused || bolFocusPaused || bolUserPaused;

  const goTo = (intIndex: number) => setIntActive((intIndex + intTotal) % intTotal);

  // Ghi thẳng CSS variable, không setState → nghiêng mượt, không render lại cả banner
  const setTilt = (numX: number, numY: number) => {
    objTiltRef.current?.style.setProperty('--tilt-x', `${(-numY * MAX_TILT_DEG).toFixed(2)}deg`);
    objTiltRef.current?.style.setProperty('--tilt-y', `${(numX * MAX_TILT_DEG * 1.6).toFixed(2)}deg`);
  };

  const handlePointerMove = (objEvent: PointerEvent<HTMLElement>) => {
    if (objEvent.pointerType !== 'mouse') return;
    const objRect = objEvent.currentTarget.getBoundingClientRect();
    setTilt(
      (objEvent.clientX - objRect.left) / objRect.width - 0.5,
      (objEvent.clientY - objRect.top) / objRect.height - 0.5,
    );
  };

  const handlePointerEnter = (objEvent: PointerEvent<HTMLElement>) => {
    if (objEvent.pointerType === 'mouse') setBolHoverPaused(true);
  };

  const handlePointerLeave = () => {
    setTilt(0, 0);
    setBolHoverPaused(false);
  };

  const handleStagePointerDown = (objEvent: PointerEvent<HTMLDivElement>) => {
    if (objEvent.pointerType !== 'mouse') objSwipeStartRef.current = objEvent.clientX;
  };

  const handleStagePointerUp = (objEvent: PointerEvent<HTMLDivElement>) => {
    if (objSwipeStartRef.current === null) return;
    const intDelta = objEvent.clientX - objSwipeStartRef.current;
    objSwipeStartRef.current = null;
    if (Math.abs(intDelta) >= SWIPE_THRESHOLD) goTo(intActive + (intDelta < 0 ? 1 : -1));
  };

  const handleKeyDown = (objEvent: KeyboardEvent<HTMLElement>) => {
    if (objEvent.key === 'ArrowRight') goTo(intActive + 1);
    if (objEvent.key === 'ArrowLeft') goTo(intActive - 1);
  };

  // Chỉ bỏ dừng khi focus rời hẳn khỏi banner
  const handleBlur = (objEvent: FocusEvent<HTMLElement>) => {
    if (!objEvent.currentTarget.contains(objEvent.relatedTarget)) setBolFocusPaused(false);
  };

  return (
    <section
      aria-roledescription="carousel"
      aria-label="Sản phẩm nổi bật"
      onPointerEnter={handlePointerEnter}
      onPointerMove={handlePointerMove}
      onPointerLeave={handlePointerLeave}
      onKeyDown={handleKeyDown}
      onFocus={() => setBolFocusPaused(true)}
      onBlur={handleBlur}
      className="relative isolate overflow-hidden bg-night text-slate-100"
    >
      <h1 className="sr-only">HPM Shop — phụ kiện công nghệ</h1>

      {/* Quầng sáng đổi màu theo slide (gradient không transition được nên chồng lớp + đổi opacity) */}
      {slides.map((x, i) => (
        <div
          key={x.id}
          aria-hidden
          className={cn(
            'pointer-events-none absolute inset-0 -z-10 transition-opacity duration-1000',
            i === intActive ? 'opacity-100' : 'opacity-0',
          )}
          style={{ background: `radial-gradient(55% 70% at 72% 35%, ${x.accent}2e 0%, transparent 70%)` }}
        />
      ))}

      {/* Sàn lưới phối cảnh chạy về phía người xem */}
      <div
        aria-hidden
        className="pointer-events-none absolute inset-x-0 bottom-0 -z-10 h-1/2 [mask-image:linear-gradient(to_top,black_10%,transparent)] [perspective:500px]"
      >
        <div
          className="absolute inset-x-[-60%] bottom-0 h-[220%] origin-bottom animate-hero-floor bg-[linear-gradient(rgba(245,196,81,0.16)_1px,transparent_1px),linear-gradient(90deg,rgba(245,196,81,0.16)_1px,transparent_1px)] bg-size-[56px_56px] motion-reduce:animate-none"
          style={{ transform: 'rotateX(76deg)' }}
        />
      </div>

      <div className="mx-auto grid max-w-7xl items-center gap-6 px-4 pt-10 pb-12 lg:grid-cols-[1fr_1.1fr] lg:gap-10 lg:pt-14 lg:pb-16">
        <div className="relative z-10 space-y-6">
          {/* Các slide chữ chồng cùng 1 ô grid, slide mới lật lên từ dưới */}
          <div aria-live={bolPaused ? 'polite' : 'off'} className="grid [perspective:900px]">
            {slides.map((x, i) => {
              const bolActive = i === intActive;
              return (
                <div
                  key={x.id}
                  role="group"
                  aria-roledescription="slide"
                  aria-label={`${i + 1} / ${intTotal}`}
                  aria-hidden={!bolActive}
                  inert={!bolActive}
                  className={cn(
                    'col-start-1 row-start-1 origin-bottom space-y-5 transition-[opacity,transform] duration-700 ease-out motion-reduce:transition-none',
                    bolActive ? 'opacity-100 delay-150' : 'pointer-events-none opacity-0',
                  )}
                  style={{ transform: bolActive ? 'none' : 'translateY(18px) rotateX(-28deg)' }}
                >
                  <span
                    className="inline-flex items-center gap-2 rounded-full border px-3 py-1 text-xs font-semibold tracking-wide uppercase"
                    style={{ borderColor: `${x.accent}66`, color: x.accent }}
                  >
                    <span className="size-1.5 rounded-full" style={{ background: x.accent }} />
                    {x.eyebrow}
                  </span>
                  <h2 className="text-4xl leading-[1.1] font-bold tracking-tight text-balance sm:text-5xl">
                    {x.title}
                  </h2>
                  <p className="max-w-xl text-lg text-slate-300">{x.description}</p>
                  <div className="flex flex-wrap gap-3">
                    <Button
                      render={<Link href={`/products/${x.product.slug}`} />}
                      nativeButton={false}
                      className="h-11 rounded-full bg-lamp px-6 text-base font-semibold text-night hover:bg-lamp-soft"
                    >
                      Mua ngay
                    </Button>
                    <Button
                      render={<Link href={x.secondaryLink.href} />}
                      nativeButton={false}
                      variant="ghost"
                      className="h-11 rounded-full px-6 text-base text-slate-100 hover:bg-white/10 hover:text-white"
                    >
                      {x.secondaryLink.label}
                    </Button>
                  </div>
                </div>
              );
            })}
          </div>

          <div className="flex items-center gap-3">
            <button
              type="button"
              aria-label="Slide trước"
              onClick={() => goTo(intActive - 1)}
              className="flex size-10 items-center justify-center rounded-full border border-white/15 text-slate-200 transition-colors hover:bg-white/10 focus-visible:ring-2 focus-visible:ring-lamp focus-visible:outline-none"
            >
              <ChevronLeft className="size-4" />
            </button>
            <div className="flex items-center gap-2">
              {slides.map((x, i) => (
                <button
                  key={x.id}
                  type="button"
                  aria-label={`Tới slide ${i + 1}: ${x.eyebrow}`}
                  aria-current={i === intActive}
                  onClick={() => goTo(i)}
                  className={cn(
                    'relative h-1.5 overflow-hidden rounded-full bg-white/15 transition-[width] duration-500 focus-visible:ring-2 focus-visible:ring-lamp focus-visible:outline-none',
                    i === intActive ? 'w-12' : 'w-4 hover:bg-white/30',
                  )}
                >
                  {/* Thanh chạy hết thì sang slide kế — dừng/tiếp tục bằng animation-play-state */}
                  {i === intActive && (
                    <span
                      key={intActive}
                      onAnimationEnd={() => goTo(intActive + 1)}
                      className="absolute inset-0 origin-left animate-hero-progress rounded-full motion-reduce:animate-none"
                      style={{ background: x.accent, animationPlayState: bolPaused ? 'paused' : 'running' }}
                    />
                  )}
                </button>
              ))}
            </div>
            <button
              type="button"
              aria-label="Slide kế"
              onClick={() => goTo(intActive + 1)}
              className="flex size-10 items-center justify-center rounded-full border border-white/15 text-slate-200 transition-colors hover:bg-white/10 focus-visible:ring-2 focus-visible:ring-lamp focus-visible:outline-none"
            >
              <ChevronRight className="size-4" />
            </button>
            <button
              type="button"
              aria-label={bolUserPaused ? 'Tiếp tục tự chuyển slide' : 'Dừng tự chuyển slide'}
              onClick={() => setBolUserPaused((x) => !x)}
              className="ml-1 flex size-8 items-center justify-center rounded-full text-slate-400 transition-colors hover:text-slate-100 focus-visible:ring-2 focus-visible:ring-lamp focus-visible:outline-none"
            >
              {bolUserPaused ? <Play className="size-3.5" /> : <Pause className="size-3.5" />}
            </button>
          </div>
        </div>

        {/* Sân khấu 3D: vuốt ngang trên mobile để đổi slide */}
        <div
          onPointerDown={handleStagePointerDown}
          onPointerUp={handleStagePointerUp}
          onPointerCancel={() => (objSwipeStartRef.current = null)}
          className="relative h-[340px] touch-pan-y [perspective:1400px] sm:h-[420px] lg:h-[480px]"
        >
          <div
            ref={objTiltRef}
            className="relative size-full transition-transform duration-500 ease-out [transform-style:preserve-3d] motion-reduce:transition-none"
            style={{ transform: 'rotateX(var(--tilt-x, 0deg)) rotateY(var(--tilt-y, 0deg))' }}
          >
            {/* Bệ đỡ phát sáng dưới thẻ chính */}
            <div
              aria-hidden
              className="absolute bottom-[4%] left-1/2 h-28 w-[min(440px,86vw)] rounded-[50%]"
              style={{
                transform: 'translateX(-50%) rotateX(80deg)',
                background: `radial-gradient(closest-side, ${slides[intActive]?.accent ?? '#f5c451'}55, transparent)`,
              }}
            />

            {slides.map((x, i) => {
              const intOffset = getOffset(i, intActive, intTotal);
              const bolFront = intOffset === 0;
              const objCard = (
                <ProductVisual
                  categorySlug={x.product.categorySlug}
                  tone={x.product.tone}
                  size="lg"
                  className="aspect-4/5 rounded-3xl shadow-[0_40px_80px_-20px_rgba(0,0,0,0.7)] ring-1 ring-white/10"
                />
              );
              return (
                <div
                  key={x.id}
                  aria-hidden={!bolFront}
                  className="absolute top-1/2 left-1/2 w-[min(280px,56vw)] transition-[transform,opacity,filter] duration-700 ease-[cubic-bezier(0.2,0.8,0.2,1)] [transform-style:preserve-3d] motion-reduce:transition-none sm:w-[300px]"
                  style={{
                    transform: getCardTransform(intOffset),
                    opacity: Math.abs(intOffset) > 1 ? 0 : bolFront ? 1 : 0.45,
                    filter: bolFront ? 'none' : 'blur(1px) saturate(0.7)',
                  }}
                >
                  <div
                    className={cn(
                      '[transform-style:preserve-3d]',
                      bolFront && 'animate-hero-float motion-reduce:animate-none',
                    )}
                  >
                    {bolFront ? (
                      <Link
                        href={`/products/${x.product.slug}`}
                        aria-label={`Xem ${x.product.name}`}
                        className="block rounded-3xl focus-visible:ring-2 focus-visible:ring-lamp focus-visible:outline-none"
                      >
                        {objCard}
                      </Link>
                    ) : (
                      <button type="button" tabIndex={-1} onClick={() => goTo(i)} className="block w-full">
                        {objCard}
                      </button>
                    )}

                    {bolFront && (
                      <>
                        {/* Chip nổi tách lớp theo trục Z → lệch thị sai khi nghiêng */}
                        <span
                          aria-hidden
                          className="absolute -top-4 -right-4 rounded-2xl px-3 py-2 text-sm font-bold text-night shadow-xl"
                          style={{ background: x.accent, transform: 'translateZ(110px)' }}
                        >
                          {x.highlight}
                        </span>
                        <div
                          className="absolute -bottom-6 -left-6 max-w-[85%] rounded-2xl bg-white p-3.5 text-ink shadow-2xl sm:-left-10"
                          style={{ transform: 'translateZ(70px)' }}
                        >
                          <p className="line-clamp-1 text-sm font-semibold">{x.product.name}</p>
                          <div className="mt-1">
                            <PriceTag price={x.product.price} oldPrice={x.product.oldPrice} />
                          </div>
                        </div>
                      </>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      </div>
    </section>
  );
}
