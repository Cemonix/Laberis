import { describe, it, expect, beforeEach, vi, afterEach } from "vitest";
import { setActivePinia, createPinia } from "pinia";
import { useAuthStore } from "../authStore";
import type { AuthTokens, UserDto, LoginDto } from "@/types/auth/auth.ts"
import { RoleEnum } from "@/types/auth/role";

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
    const mockUser: UserDto = {
        email: "test@example.com",
        userName: "testuser",
        roles: [RoleEnum.ADMIN],
    };

    const mockTokens: AuthTokens = {
        accessToken: "mock-access-token",
        expiresAt: Date.now() + 3600000, // 1 hour from now
    };

    const mockExpiredTokens: AuthTokens = {
        accessToken: "mock-expired-token",
        expiresAt: Date.now() - 1000, // Expired 1 second ago
    };

    const mockLoginDto: LoginDto = {
        email: "test@example.com",
        password: "password123",
    };

    beforeEach(() => {
        // Create a fresh pinia instance for each test
        setActivePinia(createPinia());
        authStore = useAuthStore();

        // Reset all mocks
        vi.clearAllMocks();
        
        // Use fake timers for all tests
        vi.useFakeTimers();
    });

    afterEach(() => {
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
            expect(authStore.hasRole(RoleEnum.ADMIN)).toBe(true);
            expect(authStore.hasRole(RoleEnum.USER)).toBe(false);
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


    describe("Authentication Actions", () => {
        it("should login successfully", async () => {
            const mockResponse = { user: mockUser, tokens: mockTokens };
            vi.mocked(authService.login).mockResolvedValue(mockResponse);

            await authStore.login(mockLoginDto);

            expect(authService.login).toHaveBeenCalledWith(mockLoginDto);
            expect(authStore.user).toEqual(mockUser);
            expect(authStore.tokens).toEqual(mockTokens);
            expect(authStore.isLoading).toBe(false);
        });

        it("should handle login failure", async () => {
            const error = new Error("Login failed");
            vi.mocked(authService.login).mockRejectedValue(error);

            await expect(authStore.login(mockLoginDto)).rejects.toThrow(
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

            await authStore.logout();

            expect(authService.logout).toHaveBeenCalledWith();
            expect(authStore.user).toBeNull();
            expect(authStore.tokens).toBeNull();
            expect(authStore.isLoading).toBe(false);
        });

        it("should logout even if service call fails", async () => {
            authStore.user = mockUser;
            authStore.tokens = mockTokens;

            vi.mocked(authService.logout).mockRejectedValue(
                new Error("Network error")
            );

            await authStore.logout();

            // Should still clear local state even if service call fails
            expect(authStore.user).toBeNull();
            expect(authStore.tokens).toBeNull();
        });
    });

    describe("Token Refresh", () => {
        it("should refresh tokens successfully", async () => {
            const newTokens: AuthTokens = {
                accessToken: "new-access-token",
                expiresAt: Date.now() + 3600000,
            };

            vi.mocked(authService.refreshToken).mockResolvedValue(newTokens);

            const result = await authStore.refreshTokens();

            expect(result).toBe(true);
            expect(authService.refreshToken).toHaveBeenCalledWith();
            expect(authStore.tokens).toEqual(newTokens);
        });

        it("should logout on refresh failure", async () => {
            authStore.user = mockUser;
            // Set refresh attempts to max - 1, so the next failure will trigger logout
            authStore.refreshAttempts = authStore.maxRefreshAttempts - 1;
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
        it("should try to refresh tokens on initialization", async () => {
            vi.mocked(authService.refreshToken).mockResolvedValue(mockTokens);
            vi.mocked(authService.getCurrentUser).mockResolvedValue(mockUser);

            authStore.initializeAuth();

            // Wait for the async operations to complete
            await vi.runAllTimersAsync();

            expect(authService.refreshToken).toHaveBeenCalled();
            expect(authService.getCurrentUser).toHaveBeenCalled();
            expect(authStore.tokens).toEqual(mockTokens);
            expect(authStore.user).toEqual(mockUser);
        });

        it("should handle refresh failure during initialization", async () => {
            vi.mocked(authService.refreshToken).mockRejectedValue(
                new Error("Refresh failed")
            );

            authStore.initializeAuth();

            // Wait for the async operations to complete
            await vi.runAllTimersAsync();

            expect(authStore.tokens).toBeNull();
            expect(authStore.user).toBeNull();
        });
    });
});
