import {afterEach, describe, expect, it, vi} from "vitest";

import {request} from "./http";

function mockFetchResponse(response: Response) {
    const fetchMock = vi.fn().mockResolvedValue(response);
    vi.stubGlobal("fetch", fetchMock);
    return fetchMock;
}

describe("request", () => {
    afterEach(() => {
        vi.unstubAllGlobals();
    });

    it("returns undefined for 204 responses", async () => {
        const fetchMock = mockFetchResponse(
            new Response(null, {
                status: 204,
            }),
        );

        await expect(request<void>("/api/example")).resolves.toBeUndefined();
        expect(fetchMock).toHaveBeenCalledOnce();
    });

    it("parses json responses with charset content type", async () => {
        mockFetchResponse(
            new Response(JSON.stringify({id: "city-1", name: "Neo City"}), {
                headers: {
                    "Content-Type": "application/json; charset=utf-8",
                },
                status: 200,
            }),
        );

        await expect(request<{ id: string; name: string }>("/api/city")).resolves.toEqual({
            id: "city-1",
            name: "Neo City",
        });
    });

    it("returns undefined for 200 responses with empty body", async () => {
        mockFetchResponse(
            new Response(null, {
                status: 200,
            }),
        );

        await expect(request<void>("/api/empty")).resolves.toBeUndefined();
    });

    it("returns undefined for 201 responses with empty body", async () => {
        mockFetchResponse(
            new Response(null, {
                status: 201,
            }),
        );

        await expect(request<void>("/api/created")).resolves.toBeUndefined();
    });

    it("returns text for non-json success responses", async () => {
        mockFetchResponse(
            new Response("ok", {
                headers: {
                    "Content-Type": "text/plain",
                },
                status: 200,
            }),
        );

        await expect(request<string>("/api/text")).resolves.toBe("ok");
    });

    it("parses custom json content types", async () => {
        mockFetchResponse(
            new Response(JSON.stringify({ok: true}), {
                headers: {
                    "Content-Type": "application/vnd.matrix.test+json",
                },
                status: 200,
            }),
        );

        await expect(request<{ ok: boolean }>("/api/custom-json")).resolves.toEqual({
            ok: true,
        });
    });

    it("maps non-json error responses without throwing syntax errors", async () => {
        mockFetchResponse(
            new Response("server exploded", {
                headers: {
                    "Content-Type": "text/plain",
                },
                status: 500,
            }),
        );

        await expect(request<void>("/api/error")).rejects.toMatchObject({
            message: "server exploded",
            payload: "server exploded",
            status: 500,
        });
    });
});
