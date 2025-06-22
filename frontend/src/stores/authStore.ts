import { defineStore } from "pinia";
import type {
    User,
    AuthTokens,
    LoginCredentials,
    RegisterCredentials,
} from "@/types/auth/backendAuth";
import { backendAuthService } from "@/services/auth/backendAuthService";

const AUTH_STORAGE_KEY = 'auth_tokens';

export const useAuthStore = defineStore("auth", {
    state: () => ({
        user: null as User | null,
        tokens: null as AuthTokens | null,
        isLoading: false,
        isInitialized: false,
    }),
    getters: {
        isAuthenticated(state): boolean {
            return !!(state.user && state.tokens && this.isTokenValid);
        },
        currentUser(state): User | null {
            return state.user;
        },
        hasRole(state): (role: string) => boolean {
            return (role: string) => state.user?.role === role;
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
            
            const storedTokens = this.loadTokensFromStorage();
            if (storedTokens) {
                this.tokens = storedTokens;
                try {
                    await this.getCurrentUser();
                } catch (error) {
                    console.warn("Failed to get current user during initialization:", error);
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
                console.error("Failed to save tokens to localStorage:", error);
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
                console.error("Failed to load tokens from localStorage:", error);
                this.removeTokensFromStorage();
                return null;
            }
        },
        removeTokensFromStorage(): void {
            try {
                localStorage.removeItem(AUTH_STORAGE_KEY);
            } catch (error) {
                console.error("Failed to remove tokens from localStorage:", error);
            }
        },
        async login(credentials: LoginCredentials): Promise<void> {
            this.isLoading = true;
            try {
                const response = await backendAuthService.login(credentials);

                this.user = response.user;
                this.tokens = response.tokens;

                this.saveTokensToStorage(response.tokens);
            } catch (error) {
                console.error("Login failed:", error);
                throw error;
            } finally {
                this.isLoading = false;
            }
        },
        async register(credentials: RegisterCredentials): Promise<void> {
            this.isLoading = true;
            try {
                const response = await backendAuthService.register(credentials);

                this.user = response.user;
                this.tokens = response.tokens;

                this.saveTokensToStorage(response.tokens);
            } catch (error) {
                console.error("Registration failed:", error);
                throw error;
            } finally {
                this.isLoading = false;
            }
        },
        async logout(): Promise<void> {
            this.isLoading = true;
            try {
                await backendAuthService.logout();
            } catch (error) {
                console.error("Logout request failed:", error);
            } finally {
                this.user = null;
                this.tokens = null;
                this.isInitialized = false;
                this.removeTokensFromStorage();
                this.isLoading = false;
            }
        },
        async refreshTokens(): Promise<boolean> {
            if (!this.tokens?.refreshToken) {
                return false;
            }
            try {
                const tokens = await backendAuthService.refreshToken(
                    this.tokens.refreshToken
                );
                this.tokens = tokens;
                this.saveTokensToStorage(tokens);
                return true;
            } catch (error) {
                console.error("Token refresh failed:", error);
                await this.logout();
                return false;
            }
        },
        async getCurrentUser(): Promise<void> {
            if (!this.tokens?.accessToken) {
                throw new Error("No access token available");
            }
            try {
                const userData = await backendAuthService.getCurrentUser();
                this.user = userData;
            } catch (error) {
                console.error("Failed to get current user:", error);
                throw error;
            }
        },
    },
});
