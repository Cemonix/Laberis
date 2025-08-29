/**
 * Utility functions to identify API endpoints that modify user permissions.
 * When these endpoints return successfully, permissions should be automatically refreshed.
 */

/**
 * List of API endpoints that change user permissions when called successfully.
 * These patterns support wildcards (*) and parameter matching.
 */
const PERMISSION_CHANGING_ENDPOINTS: Array<{
    pattern: string;
    methods: string[];
    description: string;
}> = [
    // Project creation - user becomes project manager
    {
        pattern: "/projects",
        methods: ["POST"],
        description: "Project creation - user becomes manager",
    },

    // Project invitation acceptance - user joins project
    {
        pattern: "/invitations/accept/*",
        methods: ["POST"],
        description: "Invitation acceptance - user joins project",
    },

    // Project member role changes
    {
        pattern: "/projects/*/projectmembers/*",
        methods: ["PUT", "PATCH"],
        description: "Project member role updates",
    },

    // Project member removal (if removing self)
    {
        pattern: "/projects/*/projectmembers/*",
        methods: ["DELETE"],
        description: "Project member removal",
    },

    // Project member additions (invitations)
    {
        pattern: "/projects/*/projectmembers",
        methods: ["POST"],
        description: "Project member addition via invitation",
    },

    // User role changes (global permissions)
    {
        pattern: "/users/*/role",
        methods: ["PUT", "PATCH"],
        description: "User global role changes",
    },

    // Account role promotions/demotions
    {
        pattern: "/account/role",
        methods: ["PUT", "PATCH"],
        description: "Account role changes",
    },
];

/**
 * Converts a pattern with wildcards to a regular expression.
 * Supports * for matching any path segment.
 *
 * @param pattern - URL pattern with wildcards (e.g., "/projects/*\/members")
 * @returns RegExp object for matching URLs
 */
function patternToRegex(pattern: string): RegExp {
    // First replace * with a placeholder to avoid escaping them
    const withPlaceholders = pattern.replace(/\*/g, '__WILDCARD__');
    // Escape special regex characters
    const escaped = withPlaceholders.replace(/[.+?^${}()|[\]\\]/g, "\\$&");
    // Replace placeholders with regex pattern for path segments
    const regexPattern = escaped.replace(/__WILDCARD__/g, "[^/]+");
    // Ensure we match the full path (anchored)
    return new RegExp(`^${regexPattern}$`);
}

/**
 * Checks if a given URL and HTTP method combination represents
 * an endpoint that changes user permissions.
 *
 * @param url - The API endpoint URL (e.g., "/projects/123/members")
 * @param method - The HTTP method (e.g., "POST", "PUT", "DELETE")
 * @returns true if this endpoint changes permissions when successful
 */
export function isPermissionChangingEndpoint(
    url: string | undefined,
    method: string
): boolean {
    if (!url || !method) {
        return false;
    }

    // Normalize the URL by removing query parameters and ensuring it starts with /
    const cleanUrl = url.split("?")[0];
    const normalizedUrl = cleanUrl.startsWith("/") ? cleanUrl : `/${cleanUrl}`;
    const upperMethod = method.toUpperCase();

    return PERMISSION_CHANGING_ENDPOINTS.some((endpoint) => {
        // Check if method matches
        if (!endpoint.methods.includes(upperMethod)) {
            return false;
        }

        // Check if URL matches pattern
        const regex = patternToRegex(endpoint.pattern);
        return regex.test(normalizedUrl);
    });
}

/**
 * Gets a human-readable description of what permission change
 * an endpoint represents, for logging purposes.
 *
 * @param url - The API endpoint URL
 * @param method - The HTTP method
 * @returns Description string or null if not a permission-changing endpoint
 */
export function getPermissionChangeDescription(
    url: string | undefined,
    method: string
): string | null {
    if (!url || !method) {
        return null;
    }

    const cleanUrl = url.split("?")[0];
    const normalizedUrl = cleanUrl.startsWith("/") ? cleanUrl : `/${cleanUrl}`;
    const upperMethod = method.toUpperCase();

    const matchingEndpoint = PERMISSION_CHANGING_ENDPOINTS.find((endpoint) => {
        if (!endpoint.methods.includes(upperMethod)) {
            return false;
        }
        const regex = patternToRegex(endpoint.pattern);
        return regex.test(normalizedUrl);
    });

    return matchingEndpoint?.description || null;
}