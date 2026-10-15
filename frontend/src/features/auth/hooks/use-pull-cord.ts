'use client';

import { useEffect, useRef, useState } from 'react';
import type { MouseEvent, PointerEvent } from 'react';

type RopePoint = { x: number; y: number; px: number; py: number };

const SEGMENT_COUNT = 12;
const SEGMENT_LENGTH = 6;
export const ROPE_LENGTH = SEGMENT_COUNT * SEGMENT_LENGTH;
/** Kéo căng thêm tối đa bao nhiêu px dưới vị trí nghỉ */
export const MAX_PULL = 56;
const GRAVITY = 0.35;
const DAMPING = 0.975;
const DAMPING_REDUCED_MOTION = 0.8;
const CONSTRAINT_ITERATIONS = 10;
const PULL_THRESHOLD = 26;
// Dịch dưới mức này coi như bấm, không phải kéo
const CLICK_TOLERANCE = 4;
// Rê chuột sát dây trong bán kính này thì dây bị gạt theo
const BRUSH_RADIUS = 16;
const BRUSH_FORCE = 0.35;
const REST_ENERGY = 0.02;
const REST_FRAMES = 40;
// Dây đang bị kéo căng mà thả ra thì giữ lại chừng này vận tốc nảy về, tránh dây văng quá đà
const RELEASE_VELOCITY_KEEP = 0.2;

export const INITIAL_CORD_PATH = `M0 0 L0 ${ROPE_LENGTH}`;

const createRope = (): RopePoint[] =>
  Array.from({ length: SEGMENT_COUNT + 1 }, (_, i) => ({
    x: 0,
    y: i * SEGMENT_LENGTH,
    px: 0,
    py: i * SEGMENT_LENGTH,
  }));

// Đường cong mượt qua trung điểm các đốt → dây cong tự nhiên, không gãy khúc
const buildPath = (lstPoint: RopePoint[]) => {
  let strPath = `M${lstPoint[0]!.x.toFixed(1)} ${lstPoint[0]!.y.toFixed(1)}`;
  for (let i = 1; i < lstPoint.length - 1; i++) {
    const a = lstPoint[i]!;
    const b = lstPoint[i + 1]!;
    strPath += ` Q${a.x.toFixed(1)} ${a.y.toFixed(1)} ${((a.x + b.x) / 2).toFixed(1)} ${((a.y + b.y) / 2).toFixed(1)}`;
  }
  const objLast = lstPoint[lstPoint.length - 1]!;
  return `${strPath} L${objLast.x.toFixed(1)} ${objLast.y.toFixed(1)}`;
};

type UsePullCordOptions = { onToggle: () => void };

/**
 * Dây kéo công tắc mô phỏng vật lý (Verlet): dây cong/trũng khi kéo, đung đưa theo hướng chuột rồi tắt dần.
 * Cập nhật DOM trực tiếp mỗi frame qua ref, không setState → không render lại component.
 */
export function usePullCord({ onToggle }: UsePullCordOptions) {
  const objSvgRef = useRef<SVGSVGElement>(null);
  const objPathRef = useRef<SVGPathElement>(null);
  const objHitPathRef = useRef<SVGPathElement>(null);
  const objKnobRef = useRef<HTMLButtonElement>(null);
  const lstPointRef = useRef<RopePoint[]>(createRope());
  const objDragRef = useRef<{ x: number; y: number; offsetX: number; offsetY: number; moved: boolean } | null>(null);
  const intFrameRef = useRef(0);
  const intRestFrameRef = useRef(0);
  const numDampingRef = useRef(DAMPING);
  const bolJustReleasedRef = useRef(false);
  const objOnToggleRef = useRef(onToggle);
  const [bolDragging, setBolDragging] = useState(false);

  useEffect(() => {
    objOnToggleRef.current = onToggle;
  }, [onToggle]);

  useEffect(() => () => cancelAnimationFrame(intFrameRef.current), []);

  const getLast = () => lstPointRef.current[lstPointRef.current.length - 1]!;

  // Toạ độ so với điểm treo dây (giữa cạnh trên của SVG)
  const toLocal = (numClientX: number, numClientY: number) => {
    const objRect = objSvgRef.current!.getBoundingClientRect();
    return { x: numClientX - objRect.left - objRect.width / 2, y: numClientY - objRect.top };
  };

  const step = () => {
    const lstPoint = lstPointRef.current;
    const objDrag = objDragRef.current;
    const objLast = getLast();
    let numEnergy = 0;

    for (let i = 1; i < lstPoint.length; i++) {
      const p = lstPoint[i]!;
      if (objDrag && p === objLast) continue;
      const numVX = (p.x - p.px) * numDampingRef.current;
      const numVY = (p.y - p.py) * numDampingRef.current;
      p.px = p.x;
      p.py = p.y;
      p.x += numVX;
      p.y += numVY + GRAVITY;
      numEnergy += numVX * numVX + numVY * numVY;
    }

    if (objDrag) {
      // Đầu dây bám theo tay, giới hạn trong tầm dây căng tối đa
      let numX = objDrag.x - objDrag.offsetX;
      let numY = objDrag.y - objDrag.offsetY;
      const numReach = ROPE_LENGTH + MAX_PULL;
      const numDistance = Math.hypot(numX, numY);
      if (numDistance > numReach) {
        numX = (numX / numDistance) * numReach;
        numY = (numY / numDistance) * numReach;
      }
      objLast.px = objLast.x;
      objLast.py = objLast.y;
      objLast.x = numX;
      objLast.y = numY;
    }

    // Frame vừa thả: giải ràng buộc kỹ hơn để dây co về hết độ căng trong 1 lần, không giật nhiều frame
    const intIterations = bolJustReleasedRef.current ? CONSTRAINT_ITERATIONS * 8 : CONSTRAINT_ITERATIONS;
    for (let k = 0; k < intIterations; k++) {
      for (let i = 0; i < lstPoint.length - 1; i++) {
        const a = lstPoint[i]!;
        const b = lstPoint[i + 1]!;
        const numDX = b.x - a.x;
        const numDY = b.y - a.y;
        const numDistance = Math.hypot(numDX, numDY) || 0.0001;
        const numDiff = (numDistance - SEGMENT_LENGTH) / numDistance;
        // Điểm treo (i = 0) và đầu dây đang bị nắm thì đứng yên, đốt còn lại tự co giãn
        const bolAPinned = i === 0;
        const bolBPinned = objDrag !== null && b === objLast;
        if (bolAPinned && bolBPinned) continue;
        const numShareA = bolAPinned ? 0 : bolBPinned ? 1 : 0.5;
        const numShareB = bolBPinned ? 0 : bolAPinned ? 1 : 0.5;
        a.x += numDX * numDiff * numShareA;
        a.y += numDY * numDiff * numShareA;
        b.x -= numDX * numDiff * numShareB;
        b.y -= numDY * numDiff * numShareB;
      }
    }

    if (bolJustReleasedRef.current) {
      bolJustReleasedRef.current = false;
      for (const p of lstPoint) {
        p.px = p.x - (p.x - p.px) * RELEASE_VELOCITY_KEEP;
        p.py = p.y - (p.y - p.py) * RELEASE_VELOCITY_KEEP;
      }
    }

    intRestFrameRef.current = !objDrag && numEnergy < REST_ENERGY ? intRestFrameRef.current + 1 : 0;
  };

  const draw = () => {
    const strPath = buildPath(lstPointRef.current);
    objPathRef.current?.setAttribute('d', strPath);
    objHitPathRef.current?.setAttribute('d', strPath);
    const objLast = getLast();
    if (objKnobRef.current)
      objKnobRef.current.style.transform = `translate(calc(${objLast.x}px - 50%), calc(${objLast.y}px - 50%))`;
  };

  const tick = () => {
    step();
    draw();
    intFrameRef.current = intRestFrameRef.current < REST_FRAMES ? requestAnimationFrame(tick) : 0;
  };

  const wake = () => {
    intRestFrameRef.current = 0;
    if (intFrameRef.current) return;
    numDampingRef.current = window.matchMedia('(prefers-reduced-motion: reduce)').matches
      ? DAMPING_REDUCED_MOTION
      : DAMPING;
    intFrameRef.current = requestAnimationFrame(tick);
  };

  // Giật nhẹ đầu dây xuống (bấm/Enter) → dây nảy lên rồi đung đưa
  const tug = () => {
    const objLast = getLast();
    objLast.y += 14;
    objLast.px += 2.5;
    wake();
  };

  const handleKnobPointerDown = (objEvent: PointerEvent<Element>) => {
    objEvent.currentTarget.setPointerCapture(objEvent.pointerId);
    const objLocal = toLocal(objEvent.clientX, objEvent.clientY);
    const objLast = getLast();
    objDragRef.current = {
      ...objLocal,
      offsetX: objLocal.x - objLast.x,
      offsetY: objLocal.y - objLast.y,
      moved: false,
    };
    setBolDragging(true);
    wake();
  };

  const handleKnobPointerMove = (objEvent: PointerEvent<Element>) => {
    const objDrag = objDragRef.current;
    if (!objDrag) return;
    const objLocal = toLocal(objEvent.clientX, objEvent.clientY);
    if (Math.hypot(objLocal.x - objDrag.x, objLocal.y - objDrag.y) > CLICK_TOLERANCE) objDrag.moved = true;
    objDrag.x = objLocal.x;
    objDrag.y = objLocal.y;
  };

  const handleKnobPointerUp = () => {
    const objDrag = objDragRef.current;
    if (!objDrag) return;
    objDragRef.current = null;
    setBolDragging(false);
    if (!objDrag.moved) {
      objOnToggleRef.current();
      return tug();
    }
    if (getLast().y - ROPE_LENGTH >= PULL_THRESHOLD) objOnToggleRef.current();
    bolJustReleasedRef.current = Math.hypot(getLast().x, getLast().y) > ROPE_LENGTH;
    wake();
  };

  // Chuột/chạm đã xử lý ở pointerup; Enter/Space từ bàn phím tạo click có detail = 0
  const handleKnobClick = (objEvent: MouseEvent<HTMLButtonElement>) => {
    if (objEvent.detail !== 0) return;
    objOnToggleRef.current();
    tug();
  };

  // Rê chuột ngang qua dây → gạt dây theo hướng chuột
  const handleBrush = (objEvent: PointerEvent<HTMLElement>) => {
    if (objDragRef.current || objEvent.pointerType !== 'mouse' || !objSvgRef.current) return;
    const objLocal = toLocal(objEvent.clientX, objEvent.clientY);
    const objHit = lstPointRef.current.find(
      (x, i) => i > 0 && Math.hypot(x.x - objLocal.x, x.y - objLocal.y) < BRUSH_RADIUS,
    );
    if (!objHit) return;
    objHit.px -= objEvent.movementX * BRUSH_FORCE;
    objHit.py -= objEvent.movementY * BRUSH_FORCE * 0.3;
    wake();
  };

  return {
    objSvgRef,
    objPathRef,
    objHitPathRef,
    objKnobRef,
    bolDragging,
    handleBrush,
    // Nắm vào thân dây hay quả nắm đều kéo được (dây bám theo tay từ đúng chỗ nắm)
    cordHandlers: {
      onPointerDown: handleKnobPointerDown,
      onPointerMove: handleKnobPointerMove,
      onPointerUp: handleKnobPointerUp,
      onPointerCancel: handleKnobPointerUp,
    },
    handleKnobClick,
  };
}
