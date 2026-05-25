// @vitest-environment jsdom

import {cleanup, render, screen} from "@testing-library/react";
import {MemoryRouter} from "react-router";
import {afterEach, describe, expect, it} from "vitest";

import NotFoundPage from "./NotFoundPage";

afterEach(() => {
    cleanup();
});

describe("NotFoundPage", () => {
    it("renders a page-not-found message", () => {
        render(
            <MemoryRouter>
                <NotFoundPage/>
            </MemoryRouter>,
        );

        expect(screen.getByText("404")).not.toBeNull();
        expect(screen.getByText("Page not found")).not.toBeNull();
    });

    it("links back to the dashboard", () => {
        render(
            <MemoryRouter>
                <NotFoundPage/>
            </MemoryRouter>,
        );

        expect(screen.getByRole("link", {name: "Back to dashboard"}).getAttribute("href")).toBe("/");
    });
});
