import apiClient from "../api/apiClient";
import { transformApiError, isValidApiResponse } from '@/services/utils';
import type { 
    LoginDto, 
    RegisterDto, 
    AuthResponseDto, 
    UserDto,
    User,
    LoginCredentials,
    RegisterCredentials,
    LoginResponse,
    RegisterResponse,
    AuthTokens
} from "@/types/auth/auth";
import { AppLogger } from "@/utils/logger";

const logger = AppLogger.createServiceLogger('BackendAuthService');

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

        const user: User = {
            id: backendResponse.user.id,
            email: backendResponse.user.email,
            userName: backendResponse.user.userName,
            createdAt: backendResponse.user.createdAt
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

    async register(credentials: RegisterCredentials): Promise<RegisterResponse> {
        logger.info(`Attempting registration for user: ${credentials.email}`);
        
        const registerDto: RegisterDto = {
            email: credentials.email,
            userName: credentials.userName,
            password: credentials.password,
            confirmPassword: credentials.confirmPassword
        };

        try {
            const response = await apiClient.post<AuthResponseDto>(
                `${this.baseUrl}/register`,
                registerDto
            );
            
            if (!isValidApiResponse(response)) {
                throw transformApiError(new Error('Invalid response data'), 
                    'Failed to register - Invalid response format');
            }

            logger.info(`Registration successful for user: ${credentials.email}`);
            return this.mapAuthResponse(response.data);
        } catch (error) {
            throw transformApiError(error, 'Failed to register');
        }
    }

    async getCurrentUser(): Promise<User> {
        logger.info('Fetching current user information');
        
        try {
            const response = await apiClient.get<UserDto>(`${this.baseUrl}/me`);
            
            if (!isValidApiResponse(response)) {
                throw transformApiError(new Error('Invalid response data'), 
                    'Failed to fetch current user - Invalid response format');
            }

            logger.info('Successfully fetched current user information');
            
            const user: User = {
                id: response.data.id,
                email: response.data.email,
                userName: response.data.userName,
                createdAt: response.data.createdAt
            };
            
            return user;
        } catch (error) {
            throw transformApiError(error, 'Failed to fetch current user');
        }
    }

    async logout(): Promise<void> {
        logger.info('Attempting logout');
        // TODO: Since the backend doesn't have a logout endpoint that requires a token,
        // we just clear the frontend state
        logger.info('Logout successful (client-side)');
    }

    // TODO: The backend doesn't have a refresh token endpoint yet
    async refreshToken(_refreshToken: string): Promise<AuthTokens> {
        throw new Error('Token refresh not implemented on backend yet');
    }
}

export const authService = new AuthService();
