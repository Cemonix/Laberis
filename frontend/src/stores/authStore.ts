import { defineStore } from "pinia";
import type {
    User,
    AuthTokens,
    LoginCredentials,
} from "@/types/auth/auth";
import { authService } from "@/services/auth/authService";

const AUTH_STORAGE_KEY = 'auth_tokens';

export const useAuthStore = defineStore("auth", {
    state: () => ({
        user: null as User | null,
        tokens: null as AuthTokens | null,
        isLoading: false,
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
    },
    actions: {
        initializeAuth(): void {
            const storedTokens = this.loadTokensFromStorage();
            if (storedTokens) {
                this.tokens = storedTokens;
                this.getCurrentUser().catch(() => {
                    this.logout();
                });
            }
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
                const response = await authService.login(credentials);

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
        async logout(): Promise<void> {
            this.isLoading = true;
            try {
                if (this.tokens) {
                    await authService.logout(this.tokens.refreshToken);
                }
            } catch (error) {
                console.error("Logout request failed:", error);
            } finally {
                this.user = null;
                this.tokens = null;
                this.removeTokensFromStorage();
                this.isLoading = false;
            }
        },
        async refreshTokens(): Promise<boolean> {
            if (!this.tokens?.refreshToken) {
                return false;
            }
            try {
                const response = await authService.refreshToken(
                    this.tokens.refreshToken
                );
                this.tokens = response.tokens;
                this.saveTokensToStorage(response.tokens);
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
                const userData = await authService.getCurrentUser();
                this.user = userData;
            } catch (error) {
                console.error("Failed to get current user:", error);
                throw error;
            }
        },
    },
});
