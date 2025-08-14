/**
 * Route constants for consistent route handling across the application
 */

// Route names that don't require authentication
export const PUBLIC_ROUTE_NAMES = [
    'Login',
    'Register', 
    'Home',
    'InviteAccept',
    'EmailVerification'
] as const;

// Route names that are specifically auth-related (login/register pages)
export const AUTH_ROUTE_NAMES = [
    'Login',
    'Register'
] as const;

// Route paths that are specifically auth-related (login/register pages)
export const AUTH_ROUTE_PATHS = [
    '/login',
    '/register'
] as const;
// Route paths that should use quick token refresh (fail fast)
export const AUTH_PUBLIC_ROUTE_PATHS = [
    '/login',
    '/register',
    '/home',
    '/verify-email',
    '/invite/accept'
] as const;

/**
 * Check if a route name is public (doesn't require authentication)
 */
export function isPublicRoute(routeName: string): boolean {
    return PUBLIC_ROUTE_NAMES.includes(routeName as typeof PUBLIC_ROUTE_NAMES[number]);
}

/**
 * Check if a route name is auth-related (login/register pages)
 */
export function isAuthRoute(routeName: string): boolean {
    return AUTH_ROUTE_NAMES.includes(routeName as typeof AUTH_ROUTE_NAMES[number]);
}

/**
 * Check if a URL path is auth-related (login/register pages)
 */
export function isAuthPath(pathname: string): boolean {
    return AUTH_ROUTE_PATHS.some(path => pathname.includes(path));
}

/**
 * Check if a URL path should use quick token refresh logic
 */
export function isAuthOrPublicPath(pathname: string): boolean {
    return AUTH_PUBLIC_ROUTE_PATHS.some(path => pathname.includes(path));
}