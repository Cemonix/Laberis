import { BaseService } from '../base';
import type { PaginatedResponse } from '@/types/api/paginatedResponse';
import type { Project } from '@/types/project/project';
import type { 
    CreateProjectRequest, 
    UpdateProjectRequest,
    ProjectStatsResponse
} from '@/types/project/requests';
import type { QueryParams } from '@/types/api';

/**
 * Service class for managing projects.
 * Extends BaseService to inherit common functionality.
 */
class ProjectService extends BaseService {
    protected readonly baseUrl = '/projects';

    constructor() {
        super('ProjectService');
    }

    /**
     * Retrieves projects with optional filtering, sorting, and pagination
     */
    async getProjects(params?: QueryParams): Promise<PaginatedResponse<Project>> {
        this.logger.info('Fetching projects...', params);
        
        const response = await this.getPaginated<Project>(this.baseUrl, params);
        
        this.logger.info(`Fetched ${response.data.length} projects (total: ${response.totalItems}).`, response);
        return response;
    }

    /**
     * Retrieves a single project by its ID
     */
    async getProject(projectId: number): Promise<Project> {
        this.logger.info(`Fetching project ${projectId}...`);
        
        const response = await this.get<Project>(this.getBaseUrl(String(projectId)));
        
        this.logger.info(`Fetched project: ${response.name} (ID: ${response.id}).`, response);
        return response;
    }

    /**
     * Creates a new project
     */
    async createProject(projectData: CreateProjectRequest): Promise<Project> {
        this.logger.info('Creating project...', projectData);
        
        const response = await this.post<CreateProjectRequest, Project>(this.baseUrl, projectData);
        
        this.logger.info(`Created project: ${response.name} (ID: ${response.id}).`, response);
        return response;
    }

    /**
     * Updates an existing project
     */
    async updateProject(projectId: number, projectData: UpdateProjectRequest): Promise<Project> {
        this.logger.info(`Updating project ${projectId}...`, projectData);
        
        const response = await this.put<UpdateProjectRequest, Project>(
            this.getBaseUrl(String(projectId)), 
            projectData
        );
        
        this.logger.info(`Updated project: ${response.name} (ID: ${projectId}).`, response);
        return response;
    }

    /**
     * Permanently deletes a project and all associated data
     */
    async deleteProject(projectId: number): Promise<void> {
        this.logger.info(`Deleting project ${projectId}...`);
        
        await this.delete(this.getBaseUrl(String(projectId)));
        
        this.logger.info(`Deleted project ${projectId} successfully.`);
    }

    /**
     * Archives a project (soft delete - changes status to archived)
     */
    async archiveProject(projectId: number): Promise<Project> {
        this.logger.info(`Archiving project ${projectId}...`);
        
        const response = await this.patch<void, Project>(
            this.getBaseUrl(`${projectId}/archive`),
            undefined
        );
        
        this.logger.info(`Archived project: ${response.name} (ID: ${projectId}).`, response);
        return response;
    }

    /**
     * Restores an archived project back to active status
     */
    async restoreProject(projectId: number): Promise<Project> {
        this.logger.info(`Restoring project ${projectId}...`);
        
        const response = await this.patch<void, Project>(
            this.getBaseUrl(`${projectId}/restore`),
            undefined
        );
        
        this.logger.info(`Restored project: ${response.name} (ID: ${projectId}).`, response);
        return response;
    }

    /**
     * Retrieves detailed statistics and progress information for a project
     */
    async getProjectStats(projectId: number): Promise<ProjectStatsResponse> {
        this.logger.info(`Fetching stats for project ${projectId}...`);
        
        const response = await this.get<ProjectStatsResponse>(
            this.getBaseUrl(`${projectId}/stats`)
        );
        
        this.logger.info(`Fetched stats for project ${projectId}.`, response);
        return response;
    }

    /**
     * Creates a duplicate of an existing project with a new name
     */
    async duplicateProject(projectId: number, newName: string): Promise<Project> {
        this.logger.info(`Duplicating project ${projectId} with name: ${newName}...`);
        
        const response = await this.post<{ name: string }, Project>(
            this.getBaseUrl(`${projectId}/duplicate`), 
            { name: newName }
        );
        
        this.logger.info(`Duplicated project: ${response.name} (ID: ${response.id}).`, response);
        return response;
    }

    /**
     * Exports project data in the specified format
     */
    async exportProject(projectId: number, format: 'json' | 'csv' | 'coco'): Promise<Blob> {
        this.logger.info(`Exporting project ${projectId} in ${format} format...`);
        
        const response = await this.get<Blob>(
            this.getBaseUrl(`${projectId}/export`),
            { 
                params: { format } as any,
                responseType: 'blob',
                validateResponse: false
            }
        );
        
        this.logger.info(`Exported project ${projectId} in ${format} format successfully.`);
        return response;
    }
}

export const projectService = new ProjectService();