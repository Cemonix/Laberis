import { describe, it, expect, beforeEach, vi, afterEach } from "vitest";
import { setActivePinia, createPinia } from "pinia";
import { useAuthStore } from "../authStore";
import type { AuthTokens, User, LoginCredentials } from "@/types/auth/auth.ts"
import { UserRole } from "@/types/auth/auth.ts";

vi.mock("@/services/auth/authService", () => ({
    authService: {
        login: vi.fn(),
        logout: vi.fn(),
        refreshToken: vi.fn(),
        getCurrentUser: vi.fn(),
    },
}));

import { authService } from "@/services/auth/authService.ts";

describe("Auth Store", () => {
    let authStore: ReturnType<typeof useAuthStore>;

    // Mock data
    const mockUser: User = {
        id: "123",
        email: "test@example.com",
        userName: "testuser",
        role: UserRole.ADMIN,
        isActive: true,
        createdAt: "2024-01-01T00:00:00Z",
        updatedAt: "2024-01-01T00:00:00Z",
    };

    const mockTokens: AuthTokens = {
        accessToken: "mock-access-token",
        refreshToken: "mock-refresh-token",
        expiresAt: Date.now() + 3600000, // 1 hour from now
    };

    const mockExpiredTokens: AuthTokens = {
        accessToken: "mock-expired-token",
        refreshToken: "mock-expired-refresh",
        expiresAt: Date.now() - 1000, // Expired 1 second ago
    };

    const mockCredentials: LoginCredentials = {
        email: "test@example.com",
        password: "password123",
    };

    beforeEach(() => {
        // Create a fresh pinia instance for each test
        setActivePinia(createPinia());
        authStore = useAuthStore();

        // Clear localStorage before each test
        localStorage.clear();

        // Reset all mocks
        vi.clearAllMocks();
        
        // Use fake timers for all tests
        vi.useFakeTimers();
    });

    afterEach(() => {
        // Clean up after each test
        localStorage.clear();
        
        // Restore real timers
        vi.useRealTimers();
    });

    describe("Initial State", () => {
        it("should have correct initial state", () => {
            expect(authStore.user).toBeNull();
            expect(authStore.tokens).toBeNull();
            expect(authStore.isLoading).toBe(false);
        });
    });

    describe("Getters", () => {
        it("should return false for isAuthenticated when no user or tokens", () => {
            expect(authStore.isAuthenticated).toBe(false);
        });

        it("should return true for isAuthenticated when user and valid tokens exist", () => {
            authStore.user = mockUser;
            authStore.tokens = mockTokens;
            expect(authStore.isAuthenticated).toBe(true);
        });

        it("should return false for isAuthenticated when tokens are expired", () => {
            authStore.user = mockUser;
            authStore.tokens = mockExpiredTokens;
            expect(authStore.isAuthenticated).toBe(false);
        });

        it("should return current user", () => {
            authStore.user = mockUser;
            expect(authStore.currentUser).toEqual(mockUser);
        });

        it("should check user role correctly", () => {
            authStore.user = mockUser;
            expect(authStore.hasRole(UserRole.ADMIN)).toBe(true);
            expect(authStore.hasRole(UserRole.VIEWER)).toBe(false);
        });

        it("should validate token expiration", () => {
            authStore.tokens = mockTokens;
            expect(authStore.isTokenValid).toBe(true);

            authStore.tokens = mockExpiredTokens;
            expect(authStore.isTokenValid).toBe(false);
        });

        it("should return access token when authenticated", () => {
            authStore.user = mockUser;
            authStore.tokens = mockTokens;
            expect(authStore.getAccessToken).toBe("mock-access-token");
        });

        it("should return null access token when not authenticated", () => {
            expect(authStore.getAccessToken).toBeNull();
        });
    });

    describe("Token Storage", () => {
        it("should save tokens to localStorage", () => {
            authStore.saveTokensToStorage(mockTokens);

            const stored = localStorage.getItem("auth_tokens");
            expect(stored).toBeTruthy();
            expect(JSON.parse(stored!)).toEqual(mockTokens);
        });

        it("should load valid tokens from localStorage", () => {
            localStorage.setItem("auth_tokens", JSON.stringify(mockTokens));

            const loaded = authStore.loadTokensFromStorage();
            expect(loaded).toEqual(mockTokens);
        });

        it("should return null and clear storage for expired tokens", () => {
            localStorage.setItem(
                "auth_tokens",
                JSON.stringify(mockExpiredTokens)
            );

            const loaded = authStore.loadTokensFromStorage();
            expect(loaded).toBeNull();
            expect(localStorage.getItem("auth_tokens")).toBeNull();
        });

        it("should handle malformed JSON in localStorage", () => {
            localStorage.setItem("auth_tokens", "invalid-json");

            const loaded = authStore.loadTokensFromStorage();
            expect(loaded).toBeNull();
            expect(localStorage.getItem("auth_tokens")).toBeNull();
        });

        it("should remove tokens from localStorage", () => {
            localStorage.setItem("auth_tokens", JSON.stringify(mockTokens));
            authStore.removeTokensFromStorage();

            expect(localStorage.getItem("auth_tokens")).toBeNull();
        });
    });

    describe("Authentication Actions", () => {
        it("should login successfully", async () => {
            const mockResponse = { user: mockUser, tokens: mockTokens };
            vi.mocked(authService.login).mockResolvedValue(mockResponse);

            await authStore.login(mockCredentials);

            expect(authService.login).toHaveBeenCalledWith(mockCredentials);
            expect(authStore.user).toEqual(mockUser);
            expect(authStore.tokens).toEqual(mockTokens);
            expect(authStore.isLoading).toBe(false);

            // Check that tokens were saved to localStorage
            const stored = localStorage.getItem("auth_tokens");
            expect(JSON.parse(stored!)).toEqual(mockTokens);
        });

        it("should handle login failure", async () => {
            const error = new Error("Login failed");
            vi.mocked(authService.login).mockRejectedValue(error);

            await expect(authStore.login(mockCredentials)).rejects.toThrow(
                "Login failed"
            );

            expect(authStore.user).toBeNull();
            expect(authStore.tokens).toBeNull();
            expect(authStore.isLoading).toBe(false);
        });

        it("should logout successfully", async () => {
            // Set up authenticated state
            authStore.user = mockUser;
            authStore.tokens = mockTokens;
            localStorage.setItem("auth_tokens", JSON.stringify(mockTokens));

            await authStore.logout();

            expect(authService.logout).toHaveBeenCalledWith(
                mockTokens.refreshToken
            );
            expect(authStore.user).toBeNull();
            expect(authStore.tokens).toBeNull();
            expect(authStore.isLoading).toBe(false);
            expect(localStorage.getItem("auth_tokens")).toBeNull();
        });

        it("should logout even if service call fails", async () => {
            authStore.user = mockUser;
            authStore.tokens = mockTokens;
            localStorage.setItem("auth_tokens", JSON.stringify(mockTokens));

            vi.mocked(authService.logout).mockRejectedValue(
                new Error("Network error")
            );

            await authStore.logout();

            // Should still clear local state even if service call fails
            expect(authStore.user).toBeNull();
            expect(authStore.tokens).toBeNull();
            expect(localStorage.getItem("auth_tokens")).toBeNull();
        });
    });

    describe("Token Refresh", () => {
        it("should refresh tokens successfully", async () => {
            const newTokens: AuthTokens = {
                accessToken: "new-access-token",
                refreshToken: "new-refresh-token",
                expiresAt: Date.now() + 3600000,
            };

            authStore.tokens = mockTokens;
            vi.mocked(authService.refreshToken).mockResolvedValue({
                accessToken: newTokens.accessToken,
                refreshToken: newTokens.refreshToken,
                expiresAt: newTokens.expiresAt,
            });

            const result = await authStore.refreshTokens();

            expect(result).toBe(true);
            expect(authService.refreshToken).toHaveBeenCalledWith(
                mockTokens.refreshToken
            );
            expect(authStore.tokens).toEqual(newTokens);

            // Check that new tokens were saved
            const stored = localStorage.getItem("auth_tokens");
            expect(JSON.parse(stored!)).toEqual(newTokens);
        });

        it("should return false when no refresh token available", async () => {
            const result = await authStore.refreshTokens();

            expect(result).toBe(false);
            expect(authService.refreshToken).not.toHaveBeenCalled();
        });

        it("should logout on refresh failure", async () => {
            authStore.tokens = mockTokens;
            authStore.user = mockUser;
            vi.mocked(authService.refreshToken).mockRejectedValue(
                new Error("Refresh failed")
            );

            const result = await authStore.refreshTokens();

            expect(result).toBe(false);
            expect(authStore.user).toBeNull();
            expect(authStore.tokens).toBeNull();
        });
    });

    describe("Get Current User", () => {
        it("should get current user successfully", async () => {
            authStore.tokens = mockTokens;
            vi.mocked(authService.getCurrentUser).mockResolvedValue(mockUser);

            await authStore.getCurrentUser();

            expect(authService.getCurrentUser).toHaveBeenCalled();
            expect(authStore.user).toEqual(mockUser);
        });

        it("should throw error when no access token", async () => {
            await expect(authStore.getCurrentUser()).rejects.toThrow(
                "No access token available"
            );
            expect(authService.getCurrentUser).not.toHaveBeenCalled();
        });

        it("should handle getCurrentUser failure", async () => {
            authStore.tokens = mockTokens;
            const error = new Error("User fetch failed");
            vi.mocked(authService.getCurrentUser).mockRejectedValue(error);

            await expect(authStore.getCurrentUser()).rejects.toThrow(
                "User fetch failed"
            );
        });
    });

    describe("Initialize Auth", () => {
        it("should initialize with valid stored tokens", async () => {
            localStorage.setItem("auth_tokens", JSON.stringify(mockTokens));
            vi.mocked(authService.getCurrentUser).mockResolvedValue(mockUser);

            authStore.initializeAuth();

            expect(authStore.tokens).toEqual(mockTokens);

            // Wait for the async getCurrentUser call
            await vi.runAllTimersAsync();

            expect(authService.getCurrentUser).toHaveBeenCalled();
        });

        it("should not initialize with expired tokens", () => {
            localStorage.setItem(
                "auth_tokens",
                JSON.stringify(mockExpiredTokens)
            );

            authStore.initializeAuth();

            expect(authStore.tokens).toBeNull();
            expect(authService.getCurrentUser).not.toHaveBeenCalled();
        });

        it("should logout if getCurrentUser fails during initialization", async () => {
            localStorage.setItem("auth_tokens", JSON.stringify(mockTokens));
            vi.mocked(authService.getCurrentUser).mockRejectedValue(
                new Error("User fetch failed")
            );

            authStore.initializeAuth();

            // Wait for the async operations to complete
            await vi.runAllTimersAsync();

            expect(authStore.tokens).toBeNull();
            expect(localStorage.getItem("auth_tokens")).toBeNull();
        });
    });
});
