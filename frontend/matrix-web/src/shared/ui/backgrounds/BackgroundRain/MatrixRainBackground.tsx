import {type CSSProperties, useMemo} from "react";
import "./matrix-rain-background.css";

type MatrixBackdropProps = {
    className?: string;

    /** 0..1 */
    rainOpacity?: number;
    /** Number of rain columns when you want to pin the layout. */
    columns?: number;
    /** Toggle individual visual layers. */
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

type MatrixBackdropStyle = CSSProperties & Record<`--${string}`, string | number>;

function clamp(value: number, min: number, max: number) {
    return Math.max(min, Math.min(max, value));
}

function randomUnit(seed: number) {
    const value = Math.sin(seed * 12.9898) * 43758.5453123;
    return value - Math.floor(value);
}

function randomInt(seed: number, min: number, max: number) {
    return Math.floor(randomUnit(seed) * (max - min + 1)) + min;
}

function pick<T>(arr: readonly T[], seed: number) {
    return arr[Math.floor(randomUnit(seed) * arr.length)];
}

function makeColumnText(length: number, seedBase: number) {
    const glyphs = [
        ..."01",
        ..."1010011010",
        ..."ｱｲｳｴｵｶｷｸｹｺｻｼｽｾｿ",
        ..."ﾊﾐﾑﾒﾓﾔﾕﾖﾗﾘﾙﾚﾛ",
        ..."∆⌃⌂⌐⌑⌒⌟⎔⎓⎖⎗",
    ];

    let value = "";

    for (let index = 0; index < length; index += 1) {
        value += pick(glyphs, seedBase + index * 1.618) + (index === length - 1 ? "" : "\n");
    }

    return value;
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
        const fallback = 34;
        if (typeof window === "undefined") {
            return columns ?? fallback;
        }

        const auto = Math.floor(window.innerWidth / 34);
        return columns ?? clamp(auto, 28, 60);
    }, [columns]);

    const rain = useMemo<RainColumn[]>(() => {
        if (!showRain) {
            return [];
        }

        return Array.from({length: cols}, (_, index) => {
            const baseSeed = cols * 101 + index * 17;
            const leftPct = clamp(
                (index / cols) * 100 + (randomUnit(baseSeed + 1) * 2 - 1),
                0,
                100,
            );
            const delaySec = -randomUnit(baseSeed + 2) * 18;
            const durationSec = randomInt(baseSeed + 3, 10, 22) + randomUnit(baseSeed + 4);
            const driftPx = Math.round((randomUnit(baseSeed + 5) * 2 - 1) * 18);
            const textLength = randomInt(baseSeed + 6, 26, 54);

            return {
                id: index,
                leftPct,
                delaySec,
                durationSec,
                driftPx,
                text: makeColumnText(textLength, baseSeed + 7),
            };
        });
    }, [cols, showRain]);

    return (
        <div
            className={`matrix-backdrop${className ? ` ${className}` : ""}`}
            style={{"--rain-opacity": rainOpacity} as MatrixBackdropStyle}
            aria-hidden="true"
        >
            {showGrid ? <div className="matrix-backdrop__grid"/> : null}
            {showVignette ? <div className="matrix-backdrop__vignette"/> : null}
            {showScanline ? <div className="matrix-backdrop__scanline"/> : null}

            {showRain ? (
                <div className="matrix-backdrop__rain">
                    {rain.map((column) => (
                        <span
                            key={column.id}
                            className="matrix-backdrop__rainColumn"
                            style={{
                                "--left": `${column.leftPct}%`,
                                "--delay": `${column.delaySec}s`,
                                "--duration": `${column.durationSec}s`,
                                "--drift": `${column.driftPx}px`,
                            } as MatrixBackdropStyle}
                        >
                            {column.text}
                        </span>
                    ))}
                </div>
            ) : null}
        </div>
    );
}
