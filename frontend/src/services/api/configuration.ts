import apiClient from "./apiClient";

/**
 * Configuration data transfer objects matching the backend
 */
export interface PermissionConfiguration {
    permissions: Record<string, Record<string, string>>;
    rolePermissions: Record<string, string[]>;
}

/**
 * Configuration API service for fetching permission rules and other configuration data
 */
export const configurationService = {
    /**
     * Fetches the permission configuration from the backend.
     * This includes permission definitions and role-based permission mappings.
     */
    async getPermissionConfiguration(): Promise<PermissionConfiguration> {
        const response = await apiClient.get<PermissionConfiguration>(
            "/api/configuration/permissions"
        );
        return response.data;
    },

    /**
     * Admin-only endpoint to reload permission configuration from file
     */
    async reloadPermissionConfiguration(): Promise<void> {
        await apiClient.post("/api/configuration/permissions/reload");
    },
};
