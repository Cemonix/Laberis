import { BaseService } from '../base/baseService';
import type { UserPermissionContext } from './permissions.types';

/**
 * Service class for managing application configuration and permissions.
 */
class PermissionService extends BaseService {
    protected readonly baseUrl = '/configuration'; // TODO: Check BE and change endpoint to permission

    constructor() {
        super('ConfigurationService');
    }

    /**
     * Gets the full user permission context including all project memberships and global permissions.
     * This is used for comprehensive permission caching on the frontend.
     */
    async getUserPermissionContext(): Promise<UserPermissionContext> {
        this.logger.info('Fetching user permission context...');
        
        const response = await this.get<UserPermissionContext>(
            this.getBaseUrl('permissions/user-context')
        );
        
        this.logger.info(
            `Fetched permission context with ${response.permissions.length} total permissions across ${Object.keys(response.projectPermissions).length} projects`
        );
        return response;
    }

    /**
     * Gets page-specific permissions for the current user.
     * Hybrid mode endpoint that provides permissions on-demand for specific pages/routes.
     */
    async getPagePermissions(page: string, projectId?: number): Promise<string[]> {
        this.logger.info(`Fetching page permissions for page '${page}'${projectId ? ` with project ${projectId}` : ''}...`);
        
        const params: Record<string, any> = { page };
        if (projectId) {
            params.projectId = projectId;
        }
        
        const response = await this.get<string[]>(
            this.getBaseUrl('permissions/page'),
            { params }
        );
        
        this.logger.info(`Fetched ${response.length} permissions for page '${page}'`);
        return response;
    }

    /**
     * Admin-only endpoint to reload permission configuration from file
     */
    async reloadPermissionConfiguration(): Promise<void> {
        this.logger.info('Reloading permission configuration...');
        
        await this.post<void, void>(this.getBaseUrl('permissions/reload'), undefined);
        
        this.logger.info('Permission configuration reloaded successfully');
    }
}

// Export singleton instance
export const permissionService = new PermissionService();
