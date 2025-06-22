// Backend-compatible auth types
export interface LoginDto {
    email: string;
    password: string;
}

export interface RegisterDto {
    email: string;
    userName: string;
    password: string;
    confirmPassword: string;
}

export interface UserDto {
    id: string;
    userName: string;
    email: string;
    createdAt: string;
}

export interface AuthResponseDto {
    token: string;
    refreshToken: string;
    expiresAt: string;
    user: UserDto;
}

export interface User {
    id: string;
    email: string;
    userName: string;
    firstName?: string;
    lastName?: string;
    role?: UserRole;
    isActive?: boolean;
    createdAt: string;
    updatedAt?: string;
}

export enum UserRole {
    ADMIN = "admin",
    MANAGER = "manager",
    ANNOTATOR = "annotator",
    VIEWER = "viewer",
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

export interface RegisterCredentials {
    email: string;
    userName: string;
    password: string;
    confirmPassword: string;
}

export interface AuthState {
    user: User | null;
    tokens: AuthTokens | null;
    isAuthenticated: boolean;
    isLoading: boolean;
}

export interface LoginResponse {
    user: User;
    tokens: AuthTokens;
}

export interface RegisterResponse {
    user: User;
    tokens: AuthTokens;
}

export interface RefreshTokenResponse {
    tokens: AuthTokens;
}
