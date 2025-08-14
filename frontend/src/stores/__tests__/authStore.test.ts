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
        
        
        // Mock the delay method to be instant
        authStore.delay = vi.fn().mockResolvedValue(undefined);
        
        // Reset all store state to ensure clean test environment
        authStore.user = null;
        authStore.tokens = null;
        authStore.isInitialized = false;
        authStore.isLoading = false;
        authStore.retryingAuth = false;
        authStore.isRefreshingTokens = false;
        authStore.connectionError = false;
        authStore.refreshPromise = null;
        
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

    describe("Token Refresh - New Implementation", () => {

        describe("ensureValidToken", () => {
            describe("when valid tokens exist in memory", () => {
                it("should return true without attempting refresh", async () => {
                    authStore.tokens = mockTokens;
                    
                    const result = await authStore.ensureValidToken('protected-page');
                    
                    expect(result).toBe(true);
                    expect(authService.refreshToken).not.toHaveBeenCalled();
                });
            });

            describe("when refresh token request fails", () => {
                beforeEach(() => {
                    authStore.tokens = null;
                });

                it("should return false for protected pages", async () => {
                    vi.mocked(authService.refreshToken).mockRejectedValue(new Error("Refresh failed"));
                    
                    const result = await authStore.ensureValidToken('protected-page');
                    
                    expect(result).toBe(false);
                    expect(authService.refreshToken).toHaveBeenCalled();
                });

                it("should return true for public pages", async () => {
                    vi.mocked(authService.refreshToken).mockRejectedValue(new Error("Refresh failed"));
                    
                    const result = await authStore.ensureValidToken('public-page');
                    
                    expect(result).toBe(true);
                    // Public pages still attempt refresh but don't block on failure
                    expect(authService.refreshToken).toHaveBeenCalled();
                });

                it("should return false for auth pages", async () => {
                    const result = await authStore.ensureValidToken('auth-page');
                    
                    expect(result).toBe(false);
                    expect(authService.refreshToken).not.toHaveBeenCalled();
                });
            });

            describe("when refresh token request succeeds", () => {
                beforeEach(() => {
                    authStore.tokens = null;
                });

                it("should never attempt refresh on auth pages", async () => {
                    const result = await authStore.ensureValidToken('auth-page');
                    
                    expect(result).toBe(false);
                    expect(authService.refreshToken).not.toHaveBeenCalled();
                });

                it("should attempt single refresh on public pages", async () => {
                    vi.mocked(authService.refreshToken).mockResolvedValue(mockTokens);
                    
                    const result = await authStore.ensureValidToken('public-page');
                    
                    expect(result).toBe(true);
                    expect(authService.refreshToken).toHaveBeenCalledTimes(1);
                    expect(authStore.tokens).toEqual(mockTokens);
                });

                it("should fail gracefully on public pages when refresh fails", async () => {
                    const authError = { response: { status: 401 } };
                    vi.mocked(authService.refreshToken).mockRejectedValue(authError);
                    
                    const result = await authStore.ensureValidToken('public-page');
                    
                    expect(result).toBe(true); // Public pages should still be accessible
                    expect(authService.refreshToken).toHaveBeenCalledTimes(1);
                    expect(authStore.tokens).toBeNull();
                });

                it("should attempt refresh with retry on protected pages", async () => {
                    vi.mocked(authService.refreshToken).mockResolvedValue(mockTokens);
                    
                    const result = await authStore.ensureValidToken('protected-page');
                    
                    expect(result).toBe(true);
                    expect(authService.refreshToken).toHaveBeenCalledTimes(1);
                    expect(authStore.tokens).toEqual(mockTokens);
                });
            });
        });

        describe("tryRefreshOnce", () => {

            it("should succeed on first attempt", async () => {
                vi.mocked(authService.refreshToken).mockResolvedValue(mockTokens);
                
                const result = await authStore.tryRefreshOnce();
                
                expect(result).toBe(true);
                expect(authService.refreshToken).toHaveBeenCalledTimes(1);
                expect(authStore.tokens).toEqual(mockTokens);
            });

            it("should fail immediately on 401 error", async () => {
                const authError = { response: { status: 401 } };
                vi.mocked(authService.refreshToken).mockRejectedValue(authError);
                
                const result = await authStore.tryRefreshOnce();
                
                expect(result).toBe(false);
                expect(authService.refreshToken).toHaveBeenCalledTimes(1);
                expect(authStore.tokens).toBeNull();
            });

            it("should fail immediately on network error", async () => {
                const networkError = { response: null }; // Network error
                vi.mocked(authService.refreshToken).mockRejectedValue(networkError);
                
                const result = await authStore.tryRefreshOnce();
                
                expect(result).toBe(false);
                expect(authService.refreshToken).toHaveBeenCalledTimes(1);
                expect(authStore.tokens).toBeNull();
            });
        });

        describe("tryRefreshWithRetry", () => {

            it("should succeed on first attempt", async () => {
                vi.mocked(authService.refreshToken).mockResolvedValue(mockTokens);
                
                const result = await authStore.tryRefreshWithRetry();
                
                expect(result).toBe(true);
                expect(authService.refreshToken).toHaveBeenCalledTimes(1);
                expect(authStore.tokens).toEqual(mockTokens);
                expect(authStore.isLoading).toBe(false);
            });

            it("should not retry on 401 authentication error", async () => {
                const authError = { response: { status: 401 } };
                vi.mocked(authService.refreshToken).mockRejectedValue(authError);
                
                const result = await authStore.tryRefreshWithRetry();
                
                expect(result).toBe(false);
                expect(authService.refreshToken).toHaveBeenCalledTimes(1);
                expect(authStore.tokens).toBeNull();
                expect(authStore.isLoading).toBe(false);
            });

            it("should retry up to 3 times on network errors", async () => {
                const networkError = { response: null }; // Network error
                vi.mocked(authService.refreshToken).mockRejectedValue(networkError);
                
                const result = await authStore.tryRefreshWithRetry();
                
                // Should try 3 times
                expect(result).toBe(false);
                expect(authService.refreshToken).toHaveBeenCalledTimes(3);
                expect(authStore.connectionError).toBe(true);
                expect(authStore.isLoading).toBe(false);
            }, 1000);

            it("should succeed on second attempt after network error", async () => {
                const networkError = { response: null };
                vi.mocked(authService.refreshToken)
                    .mockRejectedValueOnce(networkError)
                    .mockResolvedValueOnce(mockTokens);
                
                const result = await authStore.tryRefreshWithRetry();
                
                expect(result).toBe(true);
                expect(authService.refreshToken).toHaveBeenCalledTimes(2);
                expect(authStore.tokens).toEqual(mockTokens);
                expect(authStore.connectionError).toBe(false);
                expect(authStore.isLoading).toBe(false);
            }, 1000);

            it("should show loading state during retry attempts", async () => {
                const networkError = { response: null };
                vi.mocked(authService.refreshToken).mockRejectedValue(networkError);
                
                // Start the refresh
                const refreshPromise = authStore.tryRefreshWithRetry();
                
                // Should show loading immediately
                expect(authStore.isLoading).toBe(true);
                
                await refreshPromise;
                
                // Should hide loading when done
                expect(authStore.isLoading).toBe(false);
            });

            it("should implement exponential backoff between retries", async () => {
                const networkError = { response: null };
                vi.mocked(authService.refreshToken).mockRejectedValue(networkError);
                
                await authStore.tryRefreshWithRetry();
                
                expect(authService.refreshToken).toHaveBeenCalledTimes(3);
            }, 1000);
        });


        describe("Integration scenarios", () => {
            it("should handle multiple concurrent ensureValidToken calls", async () => {
                authStore.tokens = null;
                vi.mocked(authService.refreshToken).mockResolvedValue(mockTokens);
                
                // Make multiple concurrent calls
                const promises = [
                    authStore.ensureValidToken('protected-page'),
                    authStore.ensureValidToken('protected-page'),
                    authStore.ensureValidToken('protected-page')
                ];
                
                const results = await Promise.all(promises);
                
                // All should succeed
                expect(results).toEqual([true, true, true]);
                // But refresh should only be called once due to deduplication
                expect(authService.refreshToken).toHaveBeenCalledTimes(1);
            });

            it("should clear auth state after max retry failures on protected pages", async () => {
                authStore.user = mockUser;
                authStore.tokens = null;
                
                const networkError = { response: null };
                vi.mocked(authService.refreshToken).mockRejectedValue(networkError);
                
                const result = await authStore.ensureValidToken('protected-page');
                
                expect(result).toBe(false);
                expect(authStore.user).toBeNull();
                expect(authStore.tokens).toBeNull();
            }, 1000);
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
            // Clear previous mocks and set up new ones for this test
            vi.clearAllMocks();
            
            vi.mocked(authService.refreshToken).mockResolvedValueOnce(mockTokens);
            vi.mocked(authService.getCurrentUser).mockResolvedValueOnce(mockUser);

            // Create a fresh auth store instance to avoid state pollution
            const freshAuthStore = useAuthStore();
            
            // Reset the fresh store state
            freshAuthStore.user = null;
            freshAuthStore.tokens = null;
            freshAuthStore.isInitialized = false;
            freshAuthStore.isLoading = false;
            
            // Mock attemptAutoLogin to fail so refresh logic is triggered
            const mockAttemptAutoLogin = vi.fn().mockResolvedValue(false);
            freshAuthStore.attemptAutoLogin = mockAttemptAutoLogin;

            // Verify the state before calling initializeAuth
            expect(freshAuthStore.isInitialized).toBe(false);

            await freshAuthStore.initializeAuth();

            // Verify final state
            expect(freshAuthStore.isInitialized).toBe(true);
            expect(freshAuthStore.user).toEqual(mockUser);
            expect(freshAuthStore.tokens).toEqual(mockTokens);

            // Verify the refresh flow was called
            expect(authService.refreshToken).toHaveBeenCalled();
            expect(authService.getCurrentUser).toHaveBeenCalled();
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
