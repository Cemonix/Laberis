import { BaseService } from '../base/baseService';

/**
 * Base service class for project-related API services.
 * Provides common functionality for services that operate within project context.
 */
export abstract class BaseProjectService extends BaseService {
    protected readonly baseUrl = '/projects';

    /**
     * Builds URLs for project-specific endpoints.
     * Example: buildProjectUrl(123, 'annotations') -> '/projects/123/annotations'
     */
    protected buildProjectUrl(projectId: number, path: string = ''): string {
        const cleanPath = path.startsWith('/') ? path.slice(1) : path;
        const projectBase = `${this.baseUrl}/${projectId}`;
        
        if (!cleanPath) {
            return projectBase;
        }
        
        return `${projectBase}/${cleanPath}`;
    }

    /**
     * Builds URLs for nested project resources with parameter substitution.
     * Example: buildProjectResourceUrl(1, 'labelschemes/{schemeId}/labels/{labelId}', { schemeId: 2, labelId: 3 })
     * Returns: '/projects/1/labelschemes/2/labels/3'
     */
    protected buildProjectResourceUrl(
        projectId: number, 
        template: string, 
        params: Record<string, string | number> = {}
    ): string {
        const projectPath = this.buildProjectUrl(projectId, template);
        return this.buildUrl(projectPath, params);
    }
}