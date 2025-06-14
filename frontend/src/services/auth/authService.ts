import apiClient from "../api/apiClient";
import type { ApiResponse } from "@/types/api/responses";
import type { User, LoginResponse, RefreshTokenResponse, LoginCredentials } from "@/types/auth/auth";

class AuthService {
    private readonly baseUrl = "/auth";

    async login(credentials: LoginCredentials): Promise<LoginResponse> {
        const response = await apiClient.post<ApiResponse<LoginResponse>>(
            `${this.baseUrl}/login`,
            credentials
        );

        return response.data.data;
    }

    async logout(refreshToken: string): Promise<void> {
        await apiClient.post(`${this.baseUrl}/logout`, {
            refreshToken,
        });
    }

    async refreshToken(refreshToken: string): Promise<RefreshTokenResponse> {
        const response = await apiClient.post<
            ApiResponse<RefreshTokenResponse>
        >(`${this.baseUrl}/refresh`, { refreshToken });

        return response.data.data;
    }

    async getCurrentUser(): Promise<User> {
        const response = await apiClient.get<ApiResponse<User>>(
            `${this.baseUrl}/me`
        );
        return response.data.data;
    }
}

export const authService = new AuthService();
