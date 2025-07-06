import { defineStore } from "pinia";
import type {
    AuthTokens,
    LoginDto,
    RegisterDto,
    UserDto,
} from "@/types/auth/auth";
import { authService } from "@/services/auth/authService";
import { env } from "@/config/env";
import { RoleEnum } from "@/types/auth/role";
import { AppLogger } from "@/utils/logger";

const AUTH_STORAGE_KEY = 'auth_tokens';
const logger = AppLogger.createStoreLogger('AuthStore');

export const useAuthStore = defineStore("auth", {
    state: () => ({
        user: null as UserDto | null,
        tokens: null as AuthTokens | null,
        isLoading: false,
        isInitialized: false,
        refreshAttempts: 0,
        maxRefreshAttempts: 3,
    }),
    getters: {
        isAuthenticated(state): boolean {
            return !!(state.user && state.tokens && this.isTokenValid);
        },
        currentUser(state): UserDto | null {
            return state.user;
        },
        hasRole(state): (role: RoleEnum) => boolean {
            return (role: RoleEnum) => state.user?.roles?.includes(role) || false;
        },
        isTokenValid(state): boolean {
            if (!state.tokens) return false;
            return Date.now() < state.tokens.expiresAt;
        },
        getAccessToken(state): string | null {
            if (!this.isAuthenticated) {
                return null;
            }
            return state.tokens?.accessToken || null;
        },
        // Helper to determine if we have valid tokens in storage
        hasValidStoredTokens(state): boolean {
            return !!(state.tokens && this.isTokenValid);
        }
    },
    actions: {
        async initializeAuth(): Promise<void> {
            if (this.isInitialized) {
                return;
            }
            
            // In development mode, try auto-login first if no stored tokens exist
            if (env.IS_DEVELOPMENT && env.AUTO_LOGIN_DEV) {
                const storedTokens = this.loadTokensFromStorage();
                if (!storedTokens) {
                    try {
                        await this.autoLoginDev();
                        this.isInitialized = true;
                        return;
                    } catch (error) {
                        console.warn("Auto-login failed in development mode:", error);
                        // Continue with normal initialization if auto-login fails
                    }
                }
            }
            else if (!env.IS_DEVELOPMENT && env.AUTO_LOGIN_DEV) {
                logger.warn("Auto-login is only available in development mode. Skipping auto-login.");
            }
            
            const storedTokens = this.loadTokensFromStorage();
            if (storedTokens) {
                this.tokens = storedTokens;
                try {
                    await this.getCurrentUser();
                } catch (error) {
                    logger.error("Failed to get current user during initialization", error);
                    // Clear invalid tokens but don't call logout to avoid infinite loops
                    this.user = null;
                    this.tokens = null;
                    this.removeTokensFromStorage();
                }
            }
            
            this.isInitialized = true;
        },
        saveTokensToStorage(authTokens: AuthTokens): void {
            try {
                localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(authTokens));
            } catch (error) {
                logger.error("Failed to save tokens to localStorage", error);
            }
        },
        loadTokensFromStorage(): AuthTokens | null {
            try {
                const stored = localStorage.getItem(AUTH_STORAGE_KEY);
                if (!stored) return null;

                const parsed = JSON.parse(stored) as AuthTokens;

                if (Date.now() >= parsed.expiresAt) {
                    this.removeTokensFromStorage();
                    return null;
                }

                return parsed;
            } catch (error) {
                logger.error("Failed to load tokens from localStorage", error);
                this.removeTokensFromStorage();
                return null;
            }
        },
        removeTokensFromStorage(): void {
            try {
                localStorage.removeItem(AUTH_STORAGE_KEY);
            } catch (error) {
                logger.error("Failed to remove tokens from localStorage", error);
            }
        },
        async login(loginDto: LoginDto): Promise<void> {
            this.isLoading = true;
            try {
                const response = await authService.login(loginDto);

                this.user = response.user;
                this.tokens = response.tokens;

                this.saveTokensToStorage(response.tokens);
            } catch (error) {
                logger.error("Login failed", error);
                throw error;
            } finally {
                this.isLoading = false;
            }
        },
        async register(registerDto: RegisterDto): Promise<void> {
            this.isLoading = true;
            try {
                const response = await authService.register(registerDto);

                this.user = response.user;
                this.tokens = response.tokens;

                this.saveTokensToStorage(response.tokens);
            } catch (error) {
                logger.error("Registration failed", error);
                throw error;
            } finally {
                this.isLoading = false;
            }
        },
        async logout(): Promise<void> {
            this.isLoading = true;
            try {
                await authService.logout();
            } catch (error) {
                logger.error("Logout request failed", error);
            } finally {
                this.user = null;
                this.tokens = null;
                this.isInitialized = false;
                this.removeTokensFromStorage();
                this.isLoading = false;
            }
        },
        clearAuthState(): void {
            this.user = null;
            this.tokens = null;
            this.refreshAttempts = 0;
            this.removeTokensFromStorage();
        },
        async refreshTokens(): Promise<boolean> {
            if (!this.tokens?.refreshToken) {
                return false;
            }

            if (this.refreshAttempts >= this.maxRefreshAttempts) {
                logger.error("Max refresh attempts reached, logging out");
                this.clearAuthState();
                return false;
            }

            try {
                this.refreshAttempts++;
                const tokens = await authService.refreshToken(this.tokens.refreshToken);
                this.tokens = tokens;
                this.saveTokensToStorage(tokens);
                this.refreshAttempts = 0; // Reset attempts on success
                return true;
            } catch (error) {
                logger.error("Token refresh failed", error);

                if (this.refreshAttempts >= this.maxRefreshAttempts) {
                    logger.error("Max refresh attempts reached, logging out");
                    this.clearAuthState();
                }

                return false;
            }
        },
        async getCurrentUser(): Promise<void> {
            // In development mode with auto-login enabled, we can use fake tokens
            if (env.IS_DEVELOPMENT && env.AUTO_LOGIN_DEV && (!this.tokens?.accessToken || this.tokens.accessToken === "dev-fake-token")) {
                try {
                    const userData = await authService.getCurrentUser();
                    this.user = userData;
                    return;
                } catch (error) {
                    logger.error("Failed to get current user in development mode", error);
                    throw error;
                }
            }
            
            if (!this.tokens?.accessToken) {
                throw new Error("No access token available");
            }

            try {
                const userData = await authService.getCurrentUser();
                this.user = userData;
            } catch (error) {
                logger.error("Failed to get current user", error);
                throw error;
            }
        },
        
        /**
         * Auto-login for development mode using fake authentication
         */
        async autoLoginDev(): Promise<void> {
            if (!env.IS_DEVELOPMENT) {
                throw new Error("Auto-login is only available in development mode");
            }
            
            this.isLoading = true;
            try {
                // Create fake tokens for frontend compatibility first
                const fakeTokens: AuthTokens = {
                    accessToken: "dev-fake-token",
                    refreshToken: "dev-fake-refresh-token",
                    expiresAt: Date.now() + (24 * 60 * 60 * 1000), // 24 hours
                };
                
                this.tokens = fakeTokens;
                
                // Now get the current user (which will work with fake auth in dev mode)
                await this.getCurrentUser();
                
                // Don't save fake tokens to storage as they're not real
                logger.info("Auto-login successful in development mode as:", this.user?.email);
            } catch (error) {
                logger.error("Auto-login failed:", error);
                // Clean up on failure
                this.user = null;
                this.tokens = null;
                throw error;
            } finally {
                this.isLoading = false;
            }
        },
    },
});
