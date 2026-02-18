import type {ReactNode} from "react";
import MatrixBackground from "@shared/ui/backgrounds/BackgroundRain/MatrixRainBackground";
import {useWorkspacePreferences} from "@shared/theme/workspacePreferences";
import "./auth-shell.css";

type AuthShellProps = {
    children: ReactNode;
};

export default function AuthShell({children}: AuthShellProps) {
    const {preferences} = useWorkspacePreferences();
    const isAnimatedTheme = preferences.theme === "matrix";

    return (
        <div className={`auth-shell auth-shell--theme-${preferences.theme}`}>
            {isAnimatedTheme ? <MatrixBackground rainOpacity={0.3}/> : null}

            <div className="auth-shell__inner">
                {isAnimatedTheme ? (
                    <>
                        <div className="auth-shell__orb auth-shell__orb--a"/>
                        <div className="auth-shell__orb auth-shell__orb--b"/>
                    </>
                ) : null}

                {children}
            </div>
        </div>
    );
}
