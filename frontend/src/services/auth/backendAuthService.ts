import apiClient from "../api/apiClient";
import type { ApiError } from "@/types/api/error";
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
} from "@/types/auth/backendAuth";
import { AppLogger } from "@/utils/logger";

const logger = AppLogger.createServiceLogger('BackendAuthService');

class BackendAuthService {
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
            
            if (response?.data) {
                logger.info(`Login successful for user: ${credentials.email}`);
                return this.mapAuthResponse(response.data);
            } else {
                logger.error(`Invalid response structure during login for user: ${credentials.email}`);
                throw new Error('Invalid response structure from API during login.');
            }
        } catch (error) {
            const apiError = error as ApiError;
            logger.error(`Login failed for user: ${credentials.email}`, apiError.response?.data || apiError.message);
            
            // Handle specific backend error messages
            if (apiError.response?.status === 401) {
                throw new Error('Invalid email or password');
            } else if (apiError.response?.status === 400) {
                const validationErrors = apiError.response.data?.errors;
                if (validationErrors) {
                    const errorMessages = Object.values(validationErrors).flat().join(', ');
                    throw new Error(errorMessages);
                }
                throw new Error('Invalid request. Please check your input.');
            }
            
            throw new Error('Login failed. Please try again later.');
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
            
            if (response?.data) {
                logger.info(`Registration successful for user: ${credentials.email}`);
                return this.mapAuthResponse(response.data);
            } else {
                logger.error(`Invalid response structure during registration for user: ${credentials.email}`);
                throw new Error('Invalid response structure from API during registration.');
            }
        } catch (error) {
            const apiError = error as ApiError;
            logger.error(`Registration failed for user: ${credentials.email}`, apiError.response?.data || apiError.message);
            
            // Handle specific backend error messages
            if (apiError.response?.status === 400) {
                const validationErrors = apiError.response.data?.errors;
                if (validationErrors) {
                    const errorMessages = Object.values(validationErrors).flat().join(', ');
                    throw new Error(errorMessages);
                }
                throw new Error('Invalid request. Please check your input.');
            } else if (apiError.response?.status === 409) {
                throw new Error('User with this email already exists');
            }
            
            throw new Error('Registration failed. Please try again later.');
        }
    }

    async getCurrentUser(): Promise<User> {
        logger.info('Fetching current user information');
        
        try {
            const response = await apiClient.get<UserDto>(`${this.baseUrl}/me`);
            
            if (response?.data) {
                logger.info('Successfully fetched current user information');
                
                const user: User = {
                    id: response.data.id,
                    email: response.data.email,
                    userName: response.data.userName,
                    createdAt: response.data.createdAt
                };
                
                return user;
            } else {
                logger.error('Invalid response structure when fetching current user');
                throw new Error('Invalid response structure from API when fetching current user.');
            }
        } catch (error) {
            const apiError = error as ApiError;
            logger.error('Failed to fetch current user information', apiError.response?.data || apiError.message);
            
            if (apiError.response?.status === 401) {
                throw new Error('Authentication required');
            }
            
            throw new Error('Failed to fetch user information');
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

export const backendAuthService = new BackendAuthService();
