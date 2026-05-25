import {afterEach, describe, expect, it, vi} from "vitest";

import type {AuthRefreshResult} from "./http";
import {apiRequest, configureHttpAuth, HttpError, request} from "./http";

function mockFetchResponse(response: Response) {
    const fetchMock = vi.fn().mockResolvedValue(response);
    vi.stubGlobal("fetch", fetchMock);
    return fetchMock;
}

function mockFetchResponses(...responses: Response[]) {
    const fetchMock = vi.fn();

    for (const response of responses) {
        fetchMock.mockResolvedValueOnce(response);
    }

    vi.stubGlobal("fetch", fetchMock);
    return fetchMock;
}

function jsonResponse(body: unknown, status = 200): Response {
    return new Response(JSON.stringify(body), {
        headers: {
            "Content-Type": "application/json",
        },
        status,
    });
}

function emptyResponse(status: number): Response {
    return new Response(null, {
        status,
    });
}

async function waitForAssertion(assertion: () => void) {
    let lastError: unknown;

    for (let attempt = 0; attempt < 20; attempt += 1) {
        try {
            assertion();
            return;
        } catch (error) {
            lastError = error;
            await new Promise((resolve) => setTimeout(resolve, 0));
        }
    }

    throw lastError;
}

function resetHttpAuth() {
    configureHttpAuth({
        refreshToken: async () => ({
            accessToken: null,
            shouldLogout: false,
        }),
        onLogout: () => undefined,
        getAccessToken: () => null,
    });
}

afterEach(() => {
    vi.unstubAllGlobals();
    resetHttpAuth();
});

describe("request", () => {
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

describe("apiRequest", () => {
    it("attaches access token when one is available", async () => {
        const fetchMock = mockFetchResponse(jsonResponse({ok: true}));
        const getAccessToken = vi.fn(() => "initial-token");

        configureHttpAuth({
            refreshToken: vi.fn(async () => ({
                accessToken: null,
                shouldLogout: false,
            })),
            onLogout: vi.fn(),
            getAccessToken,
        });

        await expect(apiRequest<{ ok: boolean }>("/api/protected")).resolves.toEqual({
            ok: true,
        });

        const requestInit = fetchMock.mock.calls[0]?.[1] as RequestInit;
        expect(new Headers(requestInit.headers).get("Authorization")).toBe("Bearer initial-token");
    });

    it("refreshes token on 401 and retries with the new token", async () => {
        const fetchMock = mockFetchResponses(
            emptyResponse(401),
            jsonResponse({ok: true}),
        );
        const refreshToken = vi.fn(async () => ({
            accessToken: "fresh-token",
            shouldLogout: false,
        }));

        configureHttpAuth({
            refreshToken,
            onLogout: vi.fn(),
            getAccessToken: vi.fn(() => "expired-token"),
        });

        await expect(apiRequest<{ ok: boolean }>("/api/protected")).resolves.toEqual({
            ok: true,
        });

        expect(refreshToken).toHaveBeenCalledOnce();
        expect(fetchMock).toHaveBeenCalledTimes(2);

        const firstRequestInit = fetchMock.mock.calls[0]?.[1] as RequestInit;
        const retryRequestInit = fetchMock.mock.calls[1]?.[1] as RequestInit;

        expect(new Headers(firstRequestInit.headers).get("Authorization")).toBe("Bearer expired-token");
        expect(new Headers(retryRequestInit.headers).get("Authorization")).toBe("Bearer fresh-token");
    });

    it("shares one in-flight refresh across concurrent 401 responses", async () => {
        const fetchMock = mockFetchResponses(
            emptyResponse(401),
            emptyResponse(401),
            jsonResponse({request: 1}),
            jsonResponse({request: 2}),
        );
        let resolveRefresh!: (result: AuthRefreshResult) => void;
        const refreshPromise = new Promise<AuthRefreshResult>((resolve) => {
            resolveRefresh = resolve;
        });
        const refreshToken = vi.fn(() => refreshPromise);

        configureHttpAuth({
            refreshToken,
            onLogout: vi.fn(),
            getAccessToken: vi.fn(() => "expired-token"),
        });

        const firstRequest = apiRequest<{ request: number }>("/api/protected/1");
        const secondRequest = apiRequest<{ request: number }>("/api/protected/2");

        await waitForAssertion(() => {
            expect(refreshToken).toHaveBeenCalledOnce();
        });

        resolveRefresh({
            accessToken: "fresh-token",
            shouldLogout: false,
        });

        await expect(Promise.all([firstRequest, secondRequest])).resolves.toEqual([
            {request: 1},
            {request: 2},
        ]);

        expect(fetchMock).toHaveBeenCalledTimes(4);
        expect(refreshToken).toHaveBeenCalledOnce();

        const firstRetryInit = fetchMock.mock.calls[2]?.[1] as RequestInit;
        const secondRetryInit = fetchMock.mock.calls[3]?.[1] as RequestInit;

        expect(new Headers(firstRetryInit.headers).get("Authorization")).toBe("Bearer fresh-token");
        expect(new Headers(secondRetryInit.headers).get("Authorization")).toBe("Bearer fresh-token");
    });

    it("logs out and rejects original 401 when refresh returns no token", async () => {
        mockFetchResponse(emptyResponse(401));
        const logout = vi.fn();

        configureHttpAuth({
            refreshToken: vi.fn(async () => ({
                accessToken: null,
                shouldLogout: true,
            })),
            onLogout: logout,
            getAccessToken: vi.fn(() => "expired-token"),
        });

        await expect(apiRequest<void>("/api/protected")).rejects.toMatchObject({
            status: 401,
        });
        expect(logout).toHaveBeenCalledOnce();
    });

    it("logs out and rejects original 401 when refresh request is unauthorized", async () => {
        mockFetchResponse(emptyResponse(401));
        const logout = vi.fn();

        configureHttpAuth({
            refreshToken: vi.fn(async () => {
                throw new HttpError(403, "Refresh forbidden");
            }),
            onLogout: logout,
            getAccessToken: vi.fn(() => "expired-token"),
        });

        await expect(apiRequest<void>("/api/protected")).rejects.toMatchObject({
            status: 401,
        });
        expect(logout).toHaveBeenCalledOnce();
    });

    it("calls forbidden callback on 403 responses", async () => {
        mockFetchResponse(emptyResponse(403));
        const onForbidden = vi.fn();

        configureHttpAuth({
            refreshToken: vi.fn(async () => ({
                accessToken: null,
                shouldLogout: false,
            })),
            onLogout: vi.fn(),
            getAccessToken: vi.fn(() => "access-token"),
            onForbidden,
        });

        await expect(apiRequest<void>("/api/forbidden")).rejects.toMatchObject({
            status: 403,
        });
        expect(onForbidden).toHaveBeenCalledWith({
            url: "/api/forbidden",
        });
    });
});
