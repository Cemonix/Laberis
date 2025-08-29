import { BaseService } from "../base/baseService";
import type { 
    LoginDto, 
    RegisterDto, 
    AuthResponseDto, 
    UserDto,
    LoginResponse,
    RegisterResponse,
    AuthTokens,
    ChangePasswordDto,
    ChangePasswordResponse
} from "@/services/auth/auth.types";

/**
 * Service class for authentication operations.
 * Extends BaseService to inherit common functionality.
 */
class AuthService extends BaseService {
    protected readonly baseUrl = "/auth";

    constructor() {
        super('AuthService');
    }

    private mapAuthResponse(backendResponse: AuthResponseDto): LoginResponse {
        const tokens: AuthTokens = {
            accessToken: backendResponse.token,
            expiresAt: new Date(backendResponse.expiresAt).getTime()
        };

        const user: UserDto = {
            email: backendResponse.user.email,
            userName: backendResponse.user.userName,
            roles: backendResponse.user.roles
        };

        return { user, tokens };
    }

    async login(loginDto: LoginDto): Promise<LoginResponse> {
        this.logger.info(`Attempting login for user: ${loginDto.email}`);
        
        const response = await this.post<LoginDto, AuthResponseDto>(
            this.getBaseUrl('login'), 
            loginDto
        );

        this.logger.info(`Login successful for user: ${loginDto.email}`);
        return this.mapAuthResponse(response);
    }

    async register(registerDto: RegisterDto): Promise<RegisterResponse> {
        this.logger.info(`Attempting registration for user: ${registerDto.email}`);

        const response = await this.post<RegisterDto, AuthResponseDto>(
            this.getBaseUrl('register'),
            registerDto
        );

        this.logger.info(`Registration successful for user: ${registerDto.email}`);
        return this.mapAuthResponse(response);
    }

    async getCurrentUser(): Promise<UserDto> {
        this.logger.info('Fetching current user information');

        const response = await this.get<UserDto>(this.getBaseUrl('me'));

        this.logger.info('Successfully fetched current user information');
        
        const user: UserDto = {
            email: response.email,
            userName: response.userName,
            roles: response.roles
        };
        
        return user;
    }

    async logout(): Promise<void> {
        this.logger.info('Attempting logout');
        
        try {
            await this.post<void, void>(this.getBaseUrl('logout'), undefined, false);
            this.logger.info('Logout successful - tokens revoked on server');
        } catch (error) {
            this.logger.warn('Server-side logout failed, proceeding with client-side cleanup', error);
        }
    }

    async refreshToken(): Promise<AuthTokens> {
        this.logger.info('Attempting to refresh access token');
        
        const response = await this.post<null, AuthResponseDto>(
            this.getBaseUrl('refresh-token'),
            null
        );

        this.logger.info('Token refresh successful');
        
        const tokens: AuthTokens = {
            accessToken: response.token,
            expiresAt: new Date(response.expiresAt).getTime()
        };
        
        return tokens;
    }

    async changePassword(changePasswordDto: ChangePasswordDto): Promise<ChangePasswordResponse> {
        this.logger.info('Attempting to change password');
        
        const response = await this.post<ChangePasswordDto, ChangePasswordResponse>(
            this.getBaseUrl('change-password'),
            changePasswordDto
        );

        this.logger.info('Password change successful');
        return response;
    }

    async sendEmailVerification(): Promise<{ message: string }> {
        this.logger.info('Sending email verification');
        
        const response = await this.post<void, { message: string }>(
            this.getBaseUrl('send-email-verification'),
            undefined
        );

        this.logger.info('Email verification sent successfully');
        return response;
    }

    async verifyEmail(token: string): Promise<{ message: string }> {
        this.logger.info('Verifying email with token');
        
        const response = await this.post<void, { message: string }>(
            `${this.getBaseUrl('verify-email')}?token=${encodeURIComponent(token)}`,
            undefined
        );

        this.logger.info('Email verification successful');
        return response;
    }

    async resendEmailVerification(email: string): Promise<{ message: string }> {
        this.logger.info('Resending email verification');
        
        const response = await this.post<{ email: string }, { message: string }>(
            this.getBaseUrl('resend-email-verification'),
            { email }
        );

        this.logger.info('Email verification resent successfully');
        return response;
    }
}

export const authService = new AuthService();