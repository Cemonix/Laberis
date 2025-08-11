import type { RoleEnum } from "./role";

export interface LoginDto {
    email: string;
    password: string;
}

export interface LoginResponse {
    user: UserDto;
    tokens: AuthTokens;
}

export interface RegisterDto {
    email: string;
    userName: string;
    password: string;
    confirmPassword: string;
    inviteToken?: string; // Optional for registration via invite
}

export interface RegisterResponse {
    user: UserDto;
    tokens: AuthTokens;
}

export interface UserDto {
    userName: string;
    email: string;
    roles: RoleEnum[];
}

export interface AuthTokens {
    accessToken: string;
    expiresAt: number;
}

export interface RefreshTokenResponse {
    tokens: AuthTokens;
}

export interface AuthState {
    user: UserDto | null;
    tokens: AuthTokens | null;
    isAuthenticated: boolean;
    isLoading: boolean;
}

export interface AuthResponseDto {
    token: string;
    expiresAt: string;
    user: UserDto;
    // Note: refreshToken no longer sent in response body - now httpOnly cookie
}

export interface ChangePasswordDto {
    currentPassword: string;
    newPassword: string;
    confirmNewPassword: string;
}

export interface ChangePasswordResponse {
    message: string;
}