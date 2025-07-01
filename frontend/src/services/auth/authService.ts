import apiClient from "../api/apiClient";
import { transformApiError, isValidApiResponse } from '@/services/utils';
import type { 
    LoginDto, 
    RegisterDto, 
    AuthResponseDto, 
    UserDto,
    LoginCredentials,
    LoginResponse,
    RegisterResponse,
    AuthTokens
} from "@/types/auth/auth";
import { AppLogger } from "@/utils/logger";

const logger = AppLogger.createServiceLogger('AuthService');

class AuthService {
    private readonly baseUrl = "/auth";

    /**
     * Convert backend AuthResponseDto to frontend format
     */
    private mapAuthResponse(backendResponse: AuthResponseDto): LoginResponse {
        const tokens: AuthTokens = {
            accessToken: backendResponse.token,
            refreshToken: backendResponse.refreshToken,
            expiresAt: new Date(backendResponse.expiresAt).getTime()
        };

        const user: UserDto = {
            id: backendResponse.user.id,
            email: backendResponse.user.email,
            userName: backendResponse.user.userName,
            roles: backendResponse.user.roles
        };

        return { user, tokens };
    }

    async login(credentials: LoginCredentials): Promise<LoginResponse> {
        logger.info(`Attempting login for user: ${credentials.email}`);
        
        const loginDto: LoginDto = {
            email: credentials.email,
            password: credentials.password
        };

        try {
            const response = await apiClient.post<AuthResponseDto>(
                `${this.baseUrl}/login`,
                loginDto
            );
            
            if (!isValidApiResponse(response)) {
                throw transformApiError(new Error('Invalid response data'), 
                    'Failed to login - Invalid response format');
            }

            logger.info(`Login successful for user: ${credentials.email}`);
            return this.mapAuthResponse(response.data);
        } catch (error) {
            throw transformApiError(error, 'Failed to login');
        }
    }

    async register(registerDto: RegisterDto): Promise<RegisterResponse> {
        logger.info(`Attempting registration for user: ${registerDto.email}`);

        try {
            const response = await apiClient.post<AuthResponseDto>(
                `${this.baseUrl}/register`,
                registerDto
            );
            
            if (!isValidApiResponse(response)) {
                throw transformApiError(new Error('Invalid response data'), 
                    'Failed to register - Invalid response format');
            }

            logger.info(`Registration successful for user: ${registerDto.email}`);
            return this.mapAuthResponse(response.data);
        } catch (error) {
            throw transformApiError(error, 'Failed to register');
        }
    }

    async getCurrentUser(): Promise<UserDto> {
        logger.info('Fetching current user information');

        try {
            const response = await apiClient.get<UserDto>(`${this.baseUrl}/me`);
            
            if (!isValidApiResponse(response)) {
                throw transformApiError(new Error('Invalid response data'), 
                    'Failed to fetch current user - Invalid response format');
            }

            logger.info('Successfully fetched current user information');
            
            const user: UserDto = {
                id: response.data.id,
                email: response.data.email,
                userName: response.data.userName,
                roles: response.data.roles
            };
            
            return user;
        } catch (error) {
            throw transformApiError(error, 'Failed to fetch current user');
        }
    }

    async logout(): Promise<void> {
        logger.info('Attempting logout');
        
        try {
            await apiClient.post(`${this.baseUrl}/logout`);
            logger.info('Logout successful - tokens revoked on server');
        } catch (error) {
            logger.warn('Server-side logout failed, proceeding with client-side cleanup', error);
        }
    }

    async refreshToken(refreshToken: string): Promise<AuthTokens> {
        logger.info('Attempting to refresh access token');
        
        const refreshTokenRequest = {
            token: refreshToken
        };

        try {
            const response = await apiClient.post<AuthResponseDto>(
                `${this.baseUrl}/refresh-token`,
                refreshTokenRequest
            );
            
            if (!isValidApiResponse(response)) {
                throw transformApiError(new Error('Invalid response data'), 
                    'Failed to refresh token - Invalid response format');
            }

            logger.info('Token refresh successful');
            
            const tokens: AuthTokens = {
                accessToken: response.data.token,
                refreshToken: response.data.refreshToken,
                expiresAt: new Date(response.data.expiresAt).getTime()
            };
            
            return tokens;
        } catch (error) {
            logger.error('Token refresh failed', error);
            throw transformApiError(error, 'Failed to refresh token');
        }
    }
}

export const authService = new AuthService();
