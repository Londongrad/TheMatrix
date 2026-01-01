import { useEffect, useRef } from "react";
import "@services/identity/auth/styles/matrix-rain.css";

const MatrixRainBackground = () => {
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const animationRef = useRef<number | null>(null);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    const ctx = canvas.getContext("2d");
    if (!ctx) return;

    let width = window.innerWidth;
    let height = window.innerHeight;

    canvas.width = width;
    canvas.height = height;

    const fontSize = 16; // размер "символов дождя"
    const baseSpeed = 0.2; // базовая скорость "дождя"

    let columns = Math.floor(width / fontSize);
    let rows = Math.floor(height / fontSize);

    const chars = "01▲◆□■░▒▓"; // любые символы, можно изменить

    const drops: number[] = new Array(columns);
    const speeds: number[] = new Array(columns);

    const initColumns = () => {
      columns = Math.floor(width / fontSize);
      rows = Math.floor(height / fontSize);

      drops.length = columns;
      speeds.length = columns;

      for (let i = 0; i < columns; i++) {
        // стартуем сразу по всей высоте экрана, а не одной шапкой сверху
        drops[i] = Math.random() * rows;

        // чуть рандомизируем скорость для "живости"
        const speedJitter = 0.6 + Math.random() * 0.8; // 0.6..1.4
        speeds[i] = baseSpeed * speedJitter;
      }
    };

    initColumns();

    const draw = () => {
      // лёгкий «шлейф»
      ctx.fillStyle = "rgba(2, 6, 23, 0.24)"; // тёмно-синий с прозрачностью
      ctx.fillRect(0, 0, width, height);

      ctx.font = `${fontSize}px monospace`;

      for (let i = 0; i < columns; i++) {
        const text = chars.charAt(Math.floor(Math.random() * chars.length));
        const x = i * fontSize;
        const y = drops[i] * fontSize;

        // основной цвет "цифровых капель"
        ctx.fillStyle = "#38bdf8"; // голубой (можешь поменять)
        ctx.fillText(text, x, y);

        const speed = speeds[i];

        // если ушло далеко ниже экрана — респавним чуть выше верхней границы
        if (y > height + fontSize) {
          const maxOffsetRows = rows * 0.3; // до ~30% высоты экрана над верхом
          drops[i] = -Math.random() * maxOffsetRows;
        } else {
          drops[i] += speed;
        }
      }

      animationRef.current = requestAnimationFrame(draw);
    };

    draw();

    const handleResize = () => {
      width = window.innerWidth;
      height = window.innerHeight;
      canvas.width = width;
      canvas.height = height;

      initColumns();
    };

    window.addEventListener("resize", handleResize);

    return () => {
      if (animationRef.current !== null) {
        cancelAnimationFrame(animationRef.current);
      }
      window.removeEventListener("resize", handleResize);
    };
  }, []);

  return <canvas ref={canvasRef} className="matrix-rain-canvas" />;
};

export default MatrixRainBackground;
