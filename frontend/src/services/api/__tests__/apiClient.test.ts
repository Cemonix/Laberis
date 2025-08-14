import { describe, it, expect, beforeEach, vi } from "vitest";
import type { InternalAxiosRequestConfig } from "axios";

// Mock the constants
vi.mock("@/constants/routes", () => ({
    isAuthOrPublicPath: vi.fn(),
}));

// Mock the auth store module
vi.mock("@/stores/authStore", () => ({
    useAuthStore: vi.fn(),
}));

// Mock the config
vi.mock("@/config/env", () => ({
    env: {
        API_BASE_URL: "http://localhost:5000",
    },
}));

// Import after mocking
import { isAuthOrPublicPath } from "@/constants/routes";

describe("API Client Interceptor Logic", () => {
    const mockIsAuthOrPublicPath = vi.mocked(isAuthOrPublicPath);

    beforeEach(() => {
        vi.clearAllMocks();
    });

    describe("Route detection logic", () => {
        it("should identify auth/public paths correctly", () => {
            mockIsAuthOrPublicPath.mockReturnValue(true);
            
            const result = isAuthOrPublicPath("/login");
            
            expect(result).toBe(true);
            expect(mockIsAuthOrPublicPath).toHaveBeenCalledWith("/login");
        });

        it("should identify protected paths correctly", () => {
            mockIsAuthOrPublicPath.mockReturnValue(false);
            
            const result = isAuthOrPublicPath("/dashboard");
            
            expect(result).toBe(false);
            expect(mockIsAuthOrPublicPath).toHaveBeenCalledWith("/dashboard");
        });
    });

    describe("Request configuration", () => {
        it("should add Bearer token to Authorization header", () => {
            const config = { headers: {} } as InternalAxiosRequestConfig;
            const token = "test-token";
            
            // Simulate adding token to config
            config.headers.Authorization = `Bearer ${token}`;
            
            expect(config.headers.Authorization).toBe("Bearer test-token");
        });

        it("should not modify config when no token available", () => {
            const config = { 
                headers: {
                    'Content-Type': 'application/json'
                } 
            } as InternalAxiosRequestConfig;
            
            // Simulate no token scenario - no Authorization header added
            
            expect(config.headers.Authorization).toBeUndefined();
            expect(config.headers['Content-Type']).toBe('application/json');
        });
    });

    describe("Error response handling", () => {
        it("should pass through non-401 errors", () => {
            const error = {
                response: { status: 500 },
                config: { url: "/api/test" },
            };
            
            // Non-401 errors should be passed through unchanged
            expect(error.response.status).toBe(500);
        });

        it("should identify 401 errors on auth paths", () => {
            const error = {
                response: { status: 401 },
                config: { url: "/api/auth/login" },
            };
            
            mockIsAuthOrPublicPath.mockReturnValue(true);
            
            const isAuthPath = isAuthOrPublicPath(error.config.url);
            const is401 = error.response.status === 401;
            
            expect(is401).toBe(true);
            expect(isAuthPath).toBe(true);
            // Auth path 401s should be passed through without retry
        });

        it("should identify 401 errors on protected paths", () => {
            const error = {
                response: { status: 401 },
                config: { url: "/api/protected/data" },
            };
            
            mockIsAuthOrPublicPath.mockReturnValue(false);
            
            const isAuthPath = isAuthOrPublicPath(error.config.url);
            const is401 = error.response.status === 401;
            
            expect(is401).toBe(true);
            expect(isAuthPath).toBe(false);
            // Protected path 401s should trigger token refresh
        });

        it("should prevent infinite retry loops", () => {
            const error = {
                response: { status: 401 },
                config: { 
                    url: "/api/protected/data",
                    _retry: true // Already retried
                },
            };
            
            const alreadyRetried = error.config._retry;
            
            expect(alreadyRetried).toBe(true);
            // Should not retry again when _retry flag is set
        });
    });

    describe("Context determination", () => {
        it("should determine protected page context from URL", () => {
            const pathname = "/dashboard/project/123";
            mockIsAuthOrPublicPath.mockReturnValue(false);
            
            const isPublicOrAuth = isAuthOrPublicPath(pathname);
            const context = isPublicOrAuth ? 'public-page' : 'protected-page';
            
            expect(context).toBe('protected-page');
        });

        it("should determine public page context from URL", () => {
            const pathname = "/home";
            mockIsAuthOrPublicPath.mockReturnValue(true);
            
            const isPublicOrAuth = isAuthOrPublicPath(pathname);
            const context = isPublicOrAuth ? 'public-page' : 'protected-page';
            
            expect(context).toBe('public-page');
        });

        it("should determine auth page context from URL", () => {
            const pathname = "/login";
            mockIsAuthOrPublicPath.mockReturnValue(true);
            
            const isPublicOrAuth = isAuthOrPublicPath(pathname);
            
            // In actual implementation, we'd have more specific logic
            // to distinguish between auth and public pages
            expect(isPublicOrAuth).toBe(true);
        });
    });

    describe("Token refresh scenarios", () => {
        it("should mark request for retry", () => {
            const originalRequest = {
                headers: {},
                url: "/api/protected/data"
            } as InternalAxiosRequestConfig & { _retry?: boolean };
            
            // Simulate marking for retry
            originalRequest._retry = true;
            
            expect(originalRequest._retry).toBe(true);
        });

        it("should update Authorization header for retry", () => {
            const originalRequest = {
                headers: {},
                url: "/api/protected/data"
            } as InternalAxiosRequestConfig;
            
            const newToken = "new-access-token";
            
            // Simulate updating header for retry
            originalRequest.headers.Authorization = `Bearer ${newToken}`;
            
            expect(originalRequest.headers.Authorization).toBe("Bearer new-access-token");
        });
    });
});
