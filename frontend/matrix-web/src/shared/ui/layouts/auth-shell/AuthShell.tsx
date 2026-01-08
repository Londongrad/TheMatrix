import type { ReactNode } from "react";
import MatrixBackground from "@shared/ui/backgrounds/BackgroundRain/MatrixRainBackground";
import "./auth-shell.css";

type AuthShellProps = {
  children: ReactNode;
};

export default function AuthShell({ children }: AuthShellProps) {
  return (
    <div className="auth-shell">
      <MatrixBackground rainOpacity={0.3} />

      <div className="auth-shell__inner">
        <div className="auth-shell__orb auth-shell__orb--a" />
        <div className="auth-shell__orb auth-shell__orb--b" />

        {children}
      </div>
    </div>
  );
}
