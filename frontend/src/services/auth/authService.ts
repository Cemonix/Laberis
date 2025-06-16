import apiClient from "../api/apiClient";
import type { ApiError } from "@/types/api/error";
import type { User, LoginResponse, RefreshTokenResponse, LoginCredentials } from "@/types/auth/auth";
import { AppLogger } from "@/utils/logger";

const logger = AppLogger.createServiceLogger('AuthService');

class AuthService {
    private readonly baseUrl = "/auth";

    async login(credentials: LoginCredentials): Promise<LoginResponse> {
        logger.info(`Attempting login for user: ${credentials.email}`);
        try {
            const response = await apiClient.post<LoginResponse>(
                `${this.baseUrl}/login`,
                credentials
            );
            if (response && response.data && response.data.user && response.data.tokens) {
                logger.info(`Login successful for user: ${credentials.email}`, response.data.user);
                return response.data;
            } else {
                logger.error(`Invalid response structure during login for user: ${credentials.email}.`, response);
                throw new Error('Invalid response structure from API during login.');
            }
        } catch (error) {
            const apiError = error as ApiError;
            logger.error(`Login failed for user: ${credentials.email}.`, apiError.response?.data || apiError.message);
            throw apiError;
        }
    }

    async logout(refreshToken: string): Promise<void> {
        logger.info(`Attempting logout.`);
        try {
            const response = await apiClient.post(`${this.baseUrl}/logout`, {
                refreshToken,
            });
            if (response.status === 200 || response.status === 204) {
                logger.info(`Logout successful.`);
            } else {
                logger.error(`Unexpected status code during logout.`, response);
                throw new Error('Unexpected response from API during logout.');
            }
        } catch (error) {
            const apiError = error as ApiError;
            logger.error(`Logout failed.`, apiError.response?.data || apiError.message);
            throw apiError;
        }
    }

    async refreshToken(refreshTokenValue: string): Promise<RefreshTokenResponse> {
        logger.info(`Attempting to refresh token.`);
        try {
            const response = await apiClient.post<RefreshTokenResponse>
                (`${this.baseUrl}/refresh`, { refreshToken: refreshTokenValue });
            if (response && response.data && response.data.tokens) {
                logger.info(`Token refresh successful.`);
                return response.data;
            } else {
                logger.error(`Invalid response structure during token refresh.`, response);
                throw new Error('Invalid response structure from API during token refresh.');
            }
        } catch (error) {
            const apiError = error as ApiError;
            logger.error(`Token refresh failed.`, apiError.response?.data || apiError.message);
            throw apiError;
        }
    }

    async getCurrentUser(): Promise<User> {
        logger.info(`Fetching current user details.`);
        try {
            const response = await apiClient.get<User>(
                `${this.baseUrl}/me`
            );
            if (response && response.data && response.data.id) {
                logger.info(`Current user details fetched successfully.`, response.data);
                return response.data;
            } else {
                logger.error(`Invalid response structure for current user.`, response);
                throw new Error('Invalid response structure from API for current user.');
            }
        } catch (error) {
            const apiError = error as ApiError;
            logger.error(`Failed to fetch current user.`, apiError.response?.data || apiError.message);
            throw apiError;
        }
    }
}

export const authService = new AuthService();
