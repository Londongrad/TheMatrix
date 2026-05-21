import type {PropsWithChildren} from "react";
import {useLocation} from "react-router-dom";

import {AppErrorBoundary} from "./AppErrorBoundary";

export function RouteErrorBoundary({children}: PropsWithChildren) {
    const location = useLocation();

    return (
        <AppErrorBoundary resetKey={location.pathname}>
            {children}
        </AppErrorBoundary>
    );
}
