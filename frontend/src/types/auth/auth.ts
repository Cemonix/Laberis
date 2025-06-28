export enum ProjectRole {
    ADMIN = 'ADMIN',
    MANAGER = 'MANAGER',
    REVIEWER = 'REVIEWER',
    ANNOTATOR = 'ANNOTATOR',
    VIEWER = 'VIEWER',
}

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
    roles: ProjectRole[];
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

export interface RegisterCredentials {
    email: string;
    userName: string;
    password: string;
    confirmPassword: string;
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
