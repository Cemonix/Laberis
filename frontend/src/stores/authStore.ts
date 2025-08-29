import { defineStore } from "pinia";

import { authService } from "@/services/auth/authService";
import { env } from "@/config/env";
import { AppLogger } from "@/core/logger/logger";
import { LastProjectManager } from "@/core/persistence";
import { usePermissionStore } from "./permissionStore";
import { isUnauthorizedError } from "@/core/errors/errors";
import type { AuthTokens, LoginDto, RegisterDto, RoleEnum, UserDto } from "@/services/auth/auth.types";

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
        isRefreshingTokens: false,
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

                // Always attempt token refresh - the browser will include httpOnly cookie automatically
                const refreshSuccess = await this.tryRefreshOnce();

                if (refreshSuccess && this.tokens) {
                    logger.info("Token refresh successful, fetching user data");
                    // Only fetch user data if we successfully got tokens
                    await this.getCurrentUser();
                    
                    logger.info("User data fetched successfully, loading permissions");
                    // Load permissions after getting user data
                    try {
                        const permissionStore = usePermissionStore();
                        await permissionStore.loadUserPermissions();
                        logger.info("User permissions loaded during auth initialization");
                    } catch (permissionError) {
                        logger.warn("Failed to load permissions during initialization", permissionError);
                        // Don't fail initialization if permissions fail to load
                    }
                } else {
                    logger.info("Token refresh failed during initialization - user will start as guest");
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

                // Load user permissions after successful login
                try {
                    const permissionStore = usePermissionStore();
                    await permissionStore.loadUserPermissions();
                    logger.info("User permissions loaded after login");
                } catch (permissionError) {
                    logger.warn("Failed to load permissions after login", permissionError);
                    // Don't fail login if permissions fail to load
                }

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
                // Clear permission data on logout
                const permissionStore = usePermissionStore();
                permissionStore.clearPermissions();
                
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
            
            // Clear permission data
            const permissionStore = usePermissionStore();
            permissionStore.clearPermissions();
            
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
         * Single attempt token refresh - no retries
         */
        async tryRefreshOnce(): Promise<boolean> {
            logger.info("Attempting single token refresh");
            this.isRefreshingTokens = true;
            
            try {
                const tokens = await authService.refreshToken();
                this.tokens = tokens;
                this.refreshAttempts = 0;
                this.connectionError = false;
                logger.info("Token refresh successful (single attempt)");
                return true;
            } catch (error: any) {
                // Check for 401 errors using our custom UnauthorizedError class
                if (isUnauthorizedError(error)) {
                    logger.info("No valid refresh token available (401 error)");
                } else {
                    logger.warn("Token refresh failed with error:", error);
                }
                return false;
            } finally {
                this.isRefreshingTokens = false;
            }
        },

        /**
         * Token refresh with retry logic for protected pages
         */
        async tryRefreshWithRetry(): Promise<boolean> {
            // Prevent multiple simultaneous refresh attempts
            if (this.refreshPromise) {
                return this.refreshPromise;
            }
            
            this.refreshPromise = this.performRetryRefresh();
            
            try {
                return await this.refreshPromise;
            } finally {
                this.refreshPromise = null;
            }
        },

        /**
         * Internal method to perform the retry refresh logic
         */
        async performRetryRefresh(): Promise<boolean> {
            const maxRetries = 3;
            
            this.isLoading = true;
            this.isRefreshingTokens = true;
            this.connectionError = false;
            
            logger.info(`Starting token refresh with retry logic (max ${maxRetries} attempts)`);
            
            try {
                for (let attempt = 1; attempt <= maxRetries; attempt++) {
                    try {
                        logger.info(`Token refresh attempt ${attempt}/${maxRetries}`);
                        const tokens = await authService.refreshToken();
                        
                        this.tokens = tokens;
                        this.refreshAttempts = 0;
                        this.connectionError = false;
                        logger.info(`Token refresh successful on attempt ${attempt}/${maxRetries}`);
                        return true;
                    } catch (error: any) {
                        // Don't retry on authentication errors (invalid refresh token)
                        if (isUnauthorizedError(error)) {
                            logger.info("No valid refresh token available");
                            break;
                        }
                        
                        // Check for network errors
                        if (!error.response) {
                            logger.warn(`Network error on token refresh attempt ${attempt}/${maxRetries}`);
                            if (attempt < maxRetries) {
                                // Wait before retrying (exponential backoff)
                                await this.delay(Math.pow(2, attempt) * 1000);
                                continue;
                            } else {
                                // On final network failure, set connection error flag
                                this.connectionError = true;
                                break;
                            }
                        }
                        
                        // For other errors, don't retry
                        break;
                    }
                }
                
                return false;
            } finally {
                this.isLoading = false;
                this.isRefreshingTokens = false;
            }
        },

        /**
         * Ensure we have a valid access token before making authenticated requests
         * @param context - The context: 'auth-page', 'public-page', or 'protected-page'
         * @returns boolean - Whether the page/request should be allowed to proceed
         * 
         * Note: For 'public-page' context, this always returns true (page access allowed)
         * even if token refresh fails, because public pages should work without auth.
         * For initialization purposes, use tryRefreshOnce() directly to get actual refresh status.
         */
        async ensureValidToken(context: 'auth-page' | 'public-page' | 'protected-page' = 'protected-page'): Promise<boolean> {
            logger.info(`ensureValidToken called with context: ${context}`);

            // If we have a valid token in memory, use it
            if (this.hasValidTokensInMemory) {
                logger.info("Using valid tokens already in memory");
                return true;
            }
            
            // Handle based on context - always attempt refresh, browser will include httpOnly cookie
            switch (context) {
                case 'auth-page':
                    logger.info("Auth page context - skipping token refresh");
                    return false; // Never attempt refresh on auth pages
                    
                case 'public-page':
                    logger.info("Public page context - attempting single token refresh");
                    await this.tryRefreshOnce(); // Single attempt - don't block on failure
                    return true; // Always allow public pages
                    
                case 'protected-page':
                    logger.info("Protected page context - attempting token refresh with retry logic");
                    const success = await this.tryRefreshWithRetry(); // Retry on network errors only
                    if (!success) {
                        // Clear auth state on protected page failure
                        this.clearAuthState();
                    }
                    return success;
                    
                default:
                    return false;
            }
        },

        /**
         * Helper method for delays - can be mocked in tests
         */
        async delay(ms: number): Promise<void> {
            return new Promise(resolve => setTimeout(resolve, ms));
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
                return await this.tryRefreshWithRetry();
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
