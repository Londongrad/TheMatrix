// @vitest-environment jsdom

import {cleanup, render, screen} from "@testing-library/react";
import type {ReactNode} from "react";
import {afterEach, beforeEach, describe, expect, it, vi} from "vitest";

import {AppErrorBoundary} from "./AppErrorBoundary";

function ThrowingComponent(): ReactNode {
    throw new Error("Render failed");
}

describe("AppErrorBoundary", () => {
    beforeEach(() => {
        vi.spyOn(console, "error").mockImplementation(() => undefined);
    });

    afterEach(() => {
        cleanup();
        vi.restoreAllMocks();
    });

    it("renders children when no error is thrown", () => {
        render(
            <AppErrorBoundary>
                <div>Healthy page</div>
            </AppErrorBoundary>,
        );

        expect(screen.getByText("Healthy page")).not.toBeNull();
    });

    it("renders fallback when a child throws during render", () => {
        render(
            <AppErrorBoundary>
                <ThrowingComponent/>
            </AppErrorBoundary>,
        );

        expect(screen.getByText("Something went wrong")).not.toBeNull();
        expect(screen.getByText("Application error")).not.toBeNull();
    });

    it("resets fallback when resetKey changes", async () => {
        const {rerender} = render(
            <AppErrorBoundary resetKey="/broken">
                <ThrowingComponent/>
            </AppErrorBoundary>,
        );

        expect(screen.getByText("Something went wrong")).not.toBeNull();

        rerender(
            <AppErrorBoundary resetKey="/healthy">
                <div>Recovered page</div>
            </AppErrorBoundary>,
        );

        expect(await screen.findByText("Recovered page")).not.toBeNull();
    });
});
