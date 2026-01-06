import { useMemo } from "react";
import "./matrix-rain-background.css";

type MatrixBackdropProps = {
  className?: string;

  /** 0..1 */
  rainOpacity?: number; // default 0.45
  /** количество колонок, если хочешь зафиксировать */
  columns?: number;
  /** включить/выключить слои */
  showGrid?: boolean;
  showVignette?: boolean;
  showScanline?: boolean;
  showRain?: boolean;
};

type RainColumn = {
  id: number;
  leftPct: number;
  delaySec: number;
  durationSec: number;
  driftPx: number;
  text: string;
};

function randomInt(min: number, max: number) {
  return Math.floor(Math.random() * (max - min + 1)) + min;
}

function pick<T>(arr: T[]) {
  return arr[Math.floor(Math.random() * arr.length)];
}

function clamp(n: number, min: number, max: number) {
  return Math.max(min, Math.min(max, n));
}

function makeColumnText(length: number) {
  // Важно: символы СРАЗУ с \n -> получаем готовую вертикальную “строку”
  const glyphs = [
    ..."01",
    ..."1010011010",
    ..."アイウエオカキクケコサシスセソ",
    ..."ﾊﾐﾑﾒﾓﾔﾕﾖﾗﾘﾙﾚﾛﾜﾝ",
    ..."∆⌁⌂⌐⌑⌒⍟⎔⎓⎖⎗",
  ].flat();

  let s = "";
  for (let i = 0; i < length; i++) {
    s += pick(glyphs) + (i === length - 1 ? "" : "\n");
  }
  return s;
}

export default function MatrixBackdrop({
  className,
  rainOpacity = 0.45,
  columns,
  showGrid = true,
  showVignette = true,
  showScanline = true,
  showRain = true,
}: MatrixBackdropProps) {
  const cols = useMemo(() => {
    const fallback = 34; // как в ForbiddenPage
    if (typeof window === "undefined") return columns ?? fallback;

    // Чем шире экран — тем больше колонок (без фанатизма)
    const auto = Math.floor(window.innerWidth / 34);
    return columns ?? clamp(auto, 28, 60);
  }, [columns]);

  const rain = useMemo<RainColumn[]>(() => {
    if (!showRain) return [];

    const res: RainColumn[] = [];

    for (let i = 0; i < cols; i++) {
      // равномерно + небольшой рандомный “дрейф”
      const leftPct = clamp(
        (i / cols) * 100 + (Math.random() * 2 - 1.0),
        0,
        100
      );

      // отрицательный delay — чтобы при заходе на страницу дождь уже “шёл”
      const delaySec = -Math.random() * 18;

      // скорости разные, плавные
      const durationSec = randomInt(10, 22) + Math.random();

      // микроскопический диагональный дрейф (делает “живее”)
      const driftPx = Math.round((Math.random() * 2 - 1) * 18); // -18..18

      // длина строки (чем больше — тем “киношнее”)
      const textLen = randomInt(26, 54);

      res.push({
        id: i,
        leftPct,
        delaySec,
        durationSec,
        driftPx,
        text: makeColumnText(textLen),
      });
    }

    return res;
  }, [cols, showRain]);

  return (
    <div
      className={`matrix-backdrop${className ? ` ${className}` : ""}`}
      style={{ ["--rain-opacity" as any]: rainOpacity } as React.CSSProperties}
      aria-hidden="true"
    >
      {showGrid ? <div className="matrix-backdrop__grid" /> : null}
      {showVignette ? <div className="matrix-backdrop__vignette" /> : null}
      {showScanline ? <div className="matrix-backdrop__scanline" /> : null}

      {showRain ? (
        <div className="matrix-backdrop__rain">
          {rain.map((c) => (
            <span
              key={c.id}
              className="matrix-backdrop__rainColumn"
              style={
                {
                  ["--left" as any]: `${c.leftPct}%`,
                  ["--delay" as any]: `${c.delaySec}s`,
                  ["--duration" as any]: `${c.durationSec}s`,
                  ["--drift" as any]: `${c.driftPx}px`,
                } as React.CSSProperties
              }
            >
              {c.text}
            </span>
          ))}
        </div>
      ) : null}
    </div>
  );
}
