import {afterEach, describe, expect, it, vi} from "vitest";

import {request} from "./http";

describe("request", () => {
    afterEach(() => {
        vi.unstubAllGlobals();
    });

    it("returns undefined for 204 responses", async () => {
        const fetchMock = vi.fn().mockResolvedValue(
            new Response(null, {
                status: 204,
            }),
        );

        vi.stubGlobal("fetch", fetchMock);

        await expect(request<void>("/api/example")).resolves.toBeUndefined();
        expect(fetchMock).toHaveBeenCalledOnce();
    });
});
