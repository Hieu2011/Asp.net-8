// Hiệu ứng chuyển trang auth "tan thành bụi sáng": form cũ tan thành hạt sáng bay về phía bóng đèn,
// hạt từ bóng đèn bay xuống tụ lại thành form mới. Canvas + Web Animations API, không cần thư viện.

type Particle = {
  mode: 'out' | 'in';
  fromX: number;
  fromY: number;
  toX: number;
  toY: number;
  born: number;
  life: number;
  size: number;
  color: string;
  seed: number;
};

const STAGE_SELECTOR = '[data-auth-stage]';
const BULB_SELECTOR = '[data-lamp-bulb]';
const SPLIT_ATTRIBUTE = 'data-auth-split';
const MAX_PIECES = 24;
const MAX_PARTICLES = 650;
// Diện tích (px²) mỗi hạt đại diện — mảnh to sinh nhiều hạt hơn
const PARTICLE_AREA = 150;
const COLORS = ['#fff7e0', '#fde9a8', '#f5c451', '#ffd98a'];
const OUT_DURATION_MS = 480;
const OUT_STAGGER_MS = 35;
const IN_DELAY_MS = 380;
const IN_STAGGER_MS = 45;
const NAVIGATION_TIMEOUT_MS = 3000;

let lstParticle: Particle[] = [];
let objCanvas: HTMLCanvasElement | null = null;
let intFrame = 0;
let onRouteRendered: (() => void) | null = null;

/** Gọi trong useLayoutEffect khi pathname đổi — ẩn form mới trước khi trình duyệt vẽ rồi cho bụi tụ lại */
export const notifyAuthRouteChanged = () => {
  const handleRendered = onRouteRendered;
  onRouteRendered = null;
  handleRendered?.();
};

const randomBetween = (numMin: number, numMax: number) => numMin + Math.random() * (numMax - numMin);

const pickColor = () => COLORS[Math.floor(Math.random() * COLORS.length)] ?? '#fde9a8';

// Khối có data-auth-split (và form) thì tách tiếp con bên trong, còn lại là 1 mảnh
const collectPieces = (objStage: Element) => {
  const lstPiece: HTMLElement[] = [];
  const visit = (objElement: Element) => {
    if (!(objElement instanceof HTMLElement) || objElement.getClientRects().length === 0) return;
    if (objElement.hasAttribute(SPLIT_ATTRIBUTE) || objElement.tagName === 'FORM')
      return Array.from(objElement.children).forEach(visit);
    lstPiece.push(objElement);
  };
  Array.from(objStage.children).forEach(visit);
  return lstPiece.slice(0, MAX_PIECES);
};

const getBulbPoint = () => {
  const objRect = document.querySelector(BULB_SELECTOR)?.getBoundingClientRect();
  return objRect
    ? { x: objRect.left + objRect.width / 2, y: objRect.top + objRect.height / 2 }
    : { x: window.innerWidth / 2, y: 80 };
};

const easeInOutCubic = (numT: number) => (numT < 0.5 ? 4 * numT ** 3 : 1 - (-2 * numT + 2) ** 3 / 2);

const ensureCanvas = () => {
  if (objCanvas) return objCanvas;
  objCanvas = document.createElement('canvas');
  objCanvas.setAttribute('aria-hidden', 'true');
  Object.assign(objCanvas.style, { position: 'fixed', inset: '0', pointerEvents: 'none', zIndex: '50' });
  const numRatio = Math.min(window.devicePixelRatio || 1, 2);
  objCanvas.width = window.innerWidth * numRatio;
  objCanvas.height = window.innerHeight * numRatio;
  objCanvas.style.width = `${window.innerWidth}px`;
  objCanvas.style.height = `${window.innerHeight}px`;
  objCanvas.getContext('2d')?.scale(numRatio, numRatio);
  document.body.append(objCanvas);
  return objCanvas;
};

const drawFrame = () => {
  const objContext = objCanvas?.getContext('2d');
  if (!objCanvas || !objContext) return;
  const numNow = performance.now();
  objContext.clearRect(0, 0, objCanvas.width, objCanvas.height);
  objContext.globalCompositeOperation = 'lighter';

  lstParticle = lstParticle.filter((p) => numNow - p.born < p.life);
  for (const p of lstParticle) {
    const numAge = numNow - p.born;
    if (numAge < 0) continue;
    const numProgress = numAge / p.life;
    let numX: number;
    let numY: number;
    let numAlpha: number;
    if (p.mode === 'out') {
      // Bay dần về phía đèn, lắc nhẹ như bụi trong luồng sáng
      const numEase = 1 - (1 - numProgress) ** 2;
      numX = p.fromX + (p.toX - p.fromX) * numEase + Math.sin(numAge * 0.008 + p.seed) * 7;
      numY = p.fromY + (p.toY - p.fromY) * numEase;
      numAlpha = Math.min(1, numAge / 90) * (1 - numProgress) ** 1.4;
    } else {
      // Bay theo đường cong từ đèn tới đúng chỗ của mảnh form mới, sáng rực rồi tắt khi tới nơi
      const numEase = easeInOutCubic(numProgress);
      const numCurve = Math.sin(Math.PI * numEase) * p.seed * 40;
      numX = p.fromX + (p.toX - p.fromX) * numEase + numCurve;
      numY = p.fromY + (p.toY - p.fromY) * numEase;
      numAlpha = numProgress < 0.8 ? Math.min(1, numProgress * 4) : (1 - numProgress) / 0.2;
    }
    objContext.fillStyle = p.color;
    objContext.globalAlpha = numAlpha * 0.18;
    objContext.beginPath();
    objContext.arc(numX, numY, p.size * 3.2, 0, Math.PI * 2);
    objContext.fill();
    objContext.globalAlpha = numAlpha;
    objContext.beginPath();
    objContext.arc(numX, numY, p.size, 0, Math.PI * 2);
    objContext.fill();
  }

  if (lstParticle.length > 0 || onRouteRendered) {
    intFrame = requestAnimationFrame(drawFrame);
    return;
  }
  intFrame = 0;
  objCanvas.remove();
  objCanvas = null;
};

const startLoop = () => {
  ensureCanvas();
  if (!intFrame) intFrame = requestAnimationFrame(drawFrame);
};

// Mảnh to sinh nhiều hạt hơn, tổng không vượt MAX_PARTICLES
const spawnParticles = (lstPiece: HTMLElement[], strMode: Particle['mode']) => {
  const objBulb = getBulbPoint();
  const lstRect = lstPiece.map((x) => x.getBoundingClientRect());
  const numTotalArea = lstRect.reduce((numSum, x) => numSum + x.width * x.height, 0) || 1;
  const numScale = Math.min(1, MAX_PARTICLES / (numTotalArea / PARTICLE_AREA));
  const numNow = performance.now();

  lstRect.forEach((objRect, i) => {
    const intCount = Math.max(4, Math.round(((objRect.width * objRect.height) / PARTICLE_AREA) * numScale));
    for (let k = 0; k < intCount; k++) {
      const numX = objRect.left + Math.random() * objRect.width;
      const numY = objRect.top + Math.random() * objRect.height;
      const objCommon = { size: randomBetween(0.6, 1.9), color: pickColor() };
      lstParticle.push(
        strMode === 'out'
          ? {
              ...objCommon,
              mode: 'out',
              fromX: numX,
              fromY: numY,
              // Bay lên, lệch dần về phía bóng đèn — không tới hẳn, tắt giữa đường
              toX: numX + (objBulb.x - numX) * randomBetween(0.25, 0.6) + randomBetween(-30, 30),
              toY: numY + (objBulb.y - numY) * randomBetween(0.35, 0.75),
              born: numNow + i * OUT_STAGGER_MS + randomBetween(0, 160),
              life: randomBetween(700, 1150),
              seed: Math.random() * 10,
            }
          : {
              ...objCommon,
              mode: 'in',
              fromX: objBulb.x + randomBetween(-50, 50),
              fromY: objBulb.y + randomBetween(-10, 30),
              toX: numX,
              toY: numY,
              born: numNow + i * IN_STAGGER_MS + randomBetween(0, 160),
              life: randomBetween(IN_DELAY_MS, IN_DELAY_MS + 260),
              seed: randomBetween(-1, 1),
            },
      );
    }
  });
  startLoop();
};

const revealPieces = (objStage: Element) => {
  const lstPiece = collectPieces(objStage);
  // fill 'backwards' áp ngay từ lúc tạo → mảnh ẩn trước khi vẽ, hiện dần khi bụi bay tới
  lstPiece.forEach((x, i) =>
    x.animate(
      [
        { opacity: 0, filter: 'blur(8px)', transform: 'scale(0.98)' },
        { opacity: 1, filter: 'blur(0)', transform: 'none' },
      ],
      {
        duration: 520,
        delay: IN_DELAY_MS + i * IN_STAGGER_MS,
        easing: 'cubic-bezier(0.16,1,0.3,1)',
        fill: 'backwards',
      },
    ),
  );
  spawnParticles(lstPiece, 'in');
};

/** Chạy navigate kèm hiệu ứng bụi sáng; người dùng bật giảm chuyển động thì chuyển thẳng */
export async function navigateWithDust(navigate: () => void) {
  const objStage = document.querySelector(STAGE_SELECTOR);
  if (!objStage || window.matchMedia('(prefers-reduced-motion: reduce)').matches) return navigate();

  const lstOld = collectPieces(objStage);
  spawnParticles(lstOld, 'out');
  const lstAnimation = lstOld.map((x, i) =>
    x.animate(
      [
        { opacity: 1, filter: 'blur(0)', transform: 'none' },
        { opacity: 0, filter: 'blur(6px)', transform: 'translateY(-10px) scale(0.98)' },
      ],
      { duration: OUT_DURATION_MS, delay: i * OUT_STAGGER_MS, easing: 'cubic-bezier(0.4,0,1,1)', fill: 'forwards' },
    ),
  );
  await lstAnimation.at(-1)?.finished.catch(() => undefined);

  await new Promise<void>((resolve) => {
    // Route mới quá lâu không render → trả form cũ lại như trước, không để trang trống
    const intTimer = setTimeout(() => {
      onRouteRendered = null;
      lstAnimation.forEach((x) => x.cancel());
      resolve();
    }, NAVIGATION_TIMEOUT_MS);
    onRouteRendered = () => {
      clearTimeout(intTimer);
      revealPieces(objStage);
      resolve();
    };
    navigate();
  });
}
