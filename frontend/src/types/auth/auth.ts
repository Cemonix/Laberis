import type { RoleEnum } from "./role";

export interface LoginDto {
    email: string;
    password: string;
}

export interface RegisterDto {
    email: string;
    userName: string;
    password: string;
    confirmPassword: string;
    inviteToken?: string; // Optional for registration via invite
}

export interface UserDto {
    id: string;
    userName: string;
    email: string;
    roles: RoleEnum[];
}

export interface AuthResponseDto {
    token: string;
    refreshToken: string;
    expiresAt: string;
    user: UserDto;
}

export interface AuthTokens {
    accessToken: string;
    refreshToken: string;
    expiresAt: number;
}

export interface LoginCredentials {
    email: string;
    password: string;
}

export interface AuthState {
    user: UserDto | null;
    tokens: AuthTokens | null;
    isAuthenticated: boolean;
    isLoading: boolean;
}

export interface LoginResponse {
    user: UserDto;
    tokens: AuthTokens;
}

export interface RegisterResponse {
    user: UserDto;
    tokens: AuthTokens;
}

export interface RefreshTokenResponse {
    tokens: AuthTokens;
}
