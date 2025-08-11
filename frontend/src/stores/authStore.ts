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
        connectionError: false,
        retryingAuth: false,
        refreshPromise: null as Promise<boolean> | null,
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
            // Return token if we have valid tokens, regardless of user data
            if (this.isTokenValid && state.tokens?.accessToken) {
                return state.tokens.accessToken;
            }
            return null;
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
            
            this.isLoading = true;
            
            try {
                // In development mode, try auto-login first
                if (env.IS_DEVELOPMENT && env.AUTO_LOGIN_DEV) {
                    const autoLoginSuccess = await this.attemptAutoLogin();
                    if (autoLoginSuccess) {
                        this.isInitialized = true;
                        return;
                    }
                }
                
                // Standard initialization for both dev and prod
                // Only try to refresh tokens - don't fetch user data unless we get valid tokens
                const hasValidToken = await this.ensureValidToken();
                if (hasValidToken && this.tokens) {
                    // Only fetch user data if we successfully got tokens
                    await this.getCurrentUser();
                }
            } catch (error) {
                logger.info("Auth initialization completed without valid session", error);
                // Don't log as error - it's normal for users who aren't logged in
                this.clearAuthState();
            } finally {
                this.isLoading = false;
                this.isInitialized = true;
            }
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
            // Clear user's last project data on logout
            if (this.user?.email) {
                LastProjectManager.clearLastProject(this.user.email);
            }
            
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
            if (!this.user?.email) {
                logger.info("No user email available, redirecting to home");
                return "/home";
            }

            const lastProject = LastProjectManager.getLastProject(this.user.email);
            if (lastProject) {
                logger.info(`Redirecting to last project dashboard: ${lastProject.projectName} (ID: ${lastProject.projectId})`);
                return `/projects/${lastProject.projectId}`;
            }
            logger.info("No last project found, redirecting to home");
            return '/home';
        },
        async refreshTokens(): Promise<boolean> {
            // Prevent multiple simultaneous refresh attempts
            if (this.refreshPromise) {
                return this.refreshPromise;
            }
            
            this.refreshPromise = this.performTokenRefresh();
            
            try {
                return await this.refreshPromise;
            } finally {
                this.refreshPromise = null;
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
                // Add timeout to prevent infinite hanging
                const userData = await Promise.race([
                    authService.getCurrentUser(),
                    new Promise((_, reject) => 
                        setTimeout(() => reject(new Error('getCurrentUser timeout')), 10000)
                    )
                ]) as UserDto;
                this.user = userData;
            } catch (error) {
                logger.error("Failed to get current user", error);
                throw error;
            }
        },
        
        /**
         * Ensure we have a valid access token before making authenticated requests
         */
        async ensureValidToken(): Promise<boolean> {
            // If we have a valid token in memory, use it
            if (this.hasValidTokensInMemory) {
                return true;
            }
            
            // Otherwise, try to refresh from httpOnly cookie
            return await this.refreshTokens();
        },

        /**
         * Perform the actual token refresh with retry logic and proper error handling
         */
        async performTokenRefresh(): Promise<boolean> {
            const maxRetries = 3;
            let lastError: any;
            
            this.retryingAuth = true;
            this.connectionError = false;
            
            for (let attempt = 1; attempt <= maxRetries; attempt++) {
                try {
                    // Add timeout to prevent hanging
                    const tokens = await Promise.race([
                        authService.refreshToken(),
                        new Promise((_, reject) => 
                            setTimeout(() => reject(new Error('Token refresh timeout')), 10000)
                        )
                    ]) as AuthTokens;
                    this.tokens = tokens;
                    this.refreshAttempts = 0;
                    this.connectionError = false;
                    return true;
                } catch (error: any) {
                    lastError = error;
                    
                    // Don't retry on authentication errors (invalid refresh token or no refresh token)
                    if (error.response?.status === 401) {
                        logger.info("No valid refresh token available");
                        break;
                    }
                    
                    // Check for network errors
                    if (!error.response) {
                        logger.warn(`Network error on token refresh attempt ${attempt}/${maxRetries}`);
                        if (attempt < maxRetries) {
                            // Wait before retrying (exponential backoff)
                            await new Promise(resolve => setTimeout(resolve, Math.pow(2, attempt) * 1000));
                            continue;
                        } else {
                            // On final network failure, set connection error flag
                            this.connectionError = true;
                            this.retryingAuth = false;
                            return false;
                        }
                    }
                    
                    // For other errors, don't retry
                    break;
                }
            }
            
            logger.info("Token refresh completed without success - user not logged in");
            this.retryingAuth = false;
            // Don't clear auth state here for 401 errors - just return false
            return false;
        },

        /**
         * Attempt auto-login for development mode
         */
        async attemptAutoLogin(): Promise<boolean> {
            if (!env.IS_DEVELOPMENT) {
                return false;
            }
            
            try {
                // Try to get current user without tokens (development bypass)
                const userData = await authService.getCurrentUser();
                this.user = userData;
                // Set minimal fake tokens for frontend compatibility
                this.tokens = {
                    accessToken: "dev-fake-token",
                    expiresAt: Date.now() + (24 * 60 * 60 * 1000)
                };
                logger.info("Auto-login successful in development mode as:", this.user?.email);
                return true;
            } catch (error) {
                logger.warn("Auto-login failed, falling back to normal auth", error);
                return false;
            }
        },

        /**
         * Retry authentication after a connection error
         */
        async retryAuthentication(): Promise<boolean> {
            if (this.connectionError) {
                this.connectionError = false;
                return await this.refreshTokens();
            }
            return false;
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
