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
import { LastProjectManager } from "@/core/storage";

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
        // Helper to determine if we have valid tokens in memory
        hasValidTokensInMemory(state): boolean {
            return !!(state.tokens && this.isTokenValid);
        }
    },
    actions: {
        async initializeAuth(): Promise<void> {
            if (this.isInitialized) {
                return;
            }
            
            // In development mode, try auto-login first
            if (env.IS_DEVELOPMENT && env.AUTO_LOGIN_DEV) {
                try {
                    await this.autoLoginDev();
                    this.isInitialized = true;
                    return;
                } catch (error) {
                    console.warn("Auto-login failed in development mode:", error);
                    // Continue with normal initialization if auto-login fails
                }
            }
            else if (!env.IS_DEVELOPMENT && env.AUTO_LOGIN_DEV) {
                logger.warn("Auto-login is only available in development mode. Skipping auto-login.");
            }
            
            // Try to refresh token from httpOnly cookie
            try {
                const refreshSuccess = await this.refreshTokens();
                if (refreshSuccess) {
                    await this.getCurrentUser();
                }
            } catch (error) {
                logger.error("Failed to restore session during initialization", error);
                // Clear any invalid state
                this.user = null;
                this.tokens = null;
            }
            
            this.isInitialized = true;
        },
        async login(loginDto: LoginDto): Promise<void> {
            this.isLoading = true;
            try {
                const response = await authService.login(loginDto);

                this.user = response.user;
                this.tokens = response.tokens;

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
                await authService.register(registerDto);

                // Note: Users are not automatically logged in after registration
                // They need to verify their email first before they can log in
                // So we don't set user/tokens here
                logger.info("Registration successful - email verification required");

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
                this.isLoading = false;
            }
        },
        clearAuthState(): void {
            this.user = null;
            this.tokens = null;
            this.refreshAttempts = 0;
        },

        /**
         * Get the appropriate redirect URL after login
         * If user has a last project, redirect to project dashboard
         * Otherwise, redirect to home page
         */
        getPostLoginRedirectUrl(): string {
            const lastProject = LastProjectManager.getLastProject();
            if (lastProject) {
                logger.info(`Redirecting to last project dashboard: ${lastProject.projectName} (ID: ${lastProject.projectId})`);
                return `/projects/${lastProject.projectId}`;
            }
            logger.info("No last project found, redirecting to home");
            return '/home';
        },
        async refreshTokens(): Promise<boolean> {
            if (this.refreshAttempts >= this.maxRefreshAttempts) {
                logger.error("Max refresh attempts reached, logging out");
                this.clearAuthState();
                return false;
            }

            try {
                this.refreshAttempts++;
                const tokens = await authService.refreshToken();
                this.tokens = tokens;
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

        async sendEmailVerification(): Promise<{ message: string }> {
            this.isLoading = true;
            try {
                const response = await authService.sendEmailVerification();
                logger.info("Email verification sent successfully");
                return response;
            } catch (error) {
                logger.error("Failed to send email verification", error);
                throw error;
            } finally {
                this.isLoading = false;
            }
        },

        async verifyEmail(token: string): Promise<{ message: string }> {
            this.isLoading = true;
            try {
                const response = await authService.verifyEmail(token);
                logger.info("Email verified successfully");
                return response;
            } catch (error) {
                logger.error("Email verification failed", error);
                throw error;
            } finally {
                this.isLoading = false;
            }
        },

        async resendEmailVerification(email: string): Promise<{ message: string }> {
            this.isLoading = true;
            try {
                const response = await authService.resendEmailVerification(email);
                logger.info("Email verification resent successfully");
                return response;
            } catch (error) {
                logger.error("Failed to resend email verification", error);
                throw error;
            } finally {
                this.isLoading = false;
            }
        },
    },
});
