export interface User {
    id: number;
    email: string;
    firstName: string;
    lastName: string;
    role: UserRole;
    isActive: boolean;
    createdAt: string;
    updatedAt: string;
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

export interface RefreshTokenResponse {
    tokens: AuthTokens;
}
