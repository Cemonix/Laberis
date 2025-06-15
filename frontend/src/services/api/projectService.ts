import apiClient from './apiClient';
import type { ApiResponse, PaginatedResponse } from '@/types/api/responses';
import type { Project } from '@/types/project/project';
import type { 
    CreateProjectRequest, 
    UpdateProjectRequest, 
    ProjectListParams,
    ProjectStatsResponse
} from '@/types/project/requests';
import { loggerInstance } from '@/utils/logger';

class ProjectService {
    private readonly baseUrl = '/projects';
    private readonly log = loggerInstance.createServiceLogger('ProjectService');

    /**
     * Retrieves projects with optional filtering, sorting, and pagination
     * @param params Query parameters for filtering and pagination
     * @returns Promise resolving to paginated project list
     */
    async getProjects(params?: ProjectListParams): Promise<PaginatedResponse<Project>> {
        this.log.debug('Fetching projects', params);
        
        const response = await apiClient.get<PaginatedResponse<Project>>(this.baseUrl, { params });
        this.log.info(`Fetched ${response.data.data?.length || 0} projects`);
        return response.data;
    }

    /**
     * Retrieves a single project by its ID
     * @param projectId The unique identifier of the project
     * @returns Promise resolving to the project data
     */
    async getProject(projectId: number): Promise<Project> {
        this.log.debug(`Fetching project ${projectId}`);
        
        const response = await apiClient.get<Project>(`${this.baseUrl}/${projectId}`);
        this.log.info(`Fetched project: ${response.data.name}`);
        return response.data;
    }

    /**
     * Creates a new project
     * @param projectData The project data for creation
     * @returns Promise resolving to the created project
     */
    async createProject(projectData: CreateProjectRequest): Promise<Project> {
        this.log.debug('Creating project', projectData);
        
        const response = await apiClient.post<Project>(this.baseUrl, projectData);
        this.log.info(`Created project: ${response.data.name} (ID: ${response.data.id})`);
        return response.data;
    }

    /**
     * Updates an existing project
     * @param projectId The unique identifier of the project to update
     * @param projectData The updated project data
     * @returns Promise resolving to the updated project
     */
    async updateProject(projectId: number, projectData: UpdateProjectRequest): Promise<Project> {
        this.log.debug(`Updating project ${projectId}`, projectData);
        
        const response = await apiClient.put<Project>(`${this.baseUrl}/${projectId}`, projectData);
        this.log.info(`Updated project: ${response.data.name}`);
        return response.data;
    }

    /**
     * Permanently deletes a project and all associated data
     * @param projectId The unique identifier of the project to delete
     */
    async deleteProject(projectId: number): Promise<void> {
        this.log.debug(`Deleting project ${projectId}`);
        
        await apiClient.delete(`${this.baseUrl}/${projectId}`);
        this.log.info(`Deleted project ${projectId}`);
    }

    /**
     * Archives a project (soft delete - changes status to archived)
     * @param projectId The unique identifier of the project to archive
     * @returns Promise resolving to the archived project
     */
    async archiveProject(projectId: number): Promise<Project> {
        this.log.debug(`Archiving project ${projectId}`);
        
        const response = await apiClient.patch<ApiResponse<Project>>(`${this.baseUrl}/${projectId}/archive`);
        this.log.info(`Archived project: ${response.data.data.name}`);
        return response.data.data;
    }

    /**
     * Restores an archived project back to active status
     * @param projectId The unique identifier of the project to restore
     * @returns Promise resolving to the restored project
     */
    async restoreProject(projectId: number): Promise<Project> {
        this.log.debug(`Restoring project ${projectId}`);
        
        const response = await apiClient.patch<ApiResponse<Project>>(`${this.baseUrl}/${projectId}/restore`);
        this.log.info(`Restored project: ${response.data.data.name}`);
        return response.data.data;
    }

    /**
     * Retrieves detailed statistics and progress information for a project
     * @param projectId The unique identifier of the project
     * @returns Promise resolving to project statistics
     */
    async getProjectStats(projectId: number): Promise<ProjectStatsResponse> {
        this.log.debug(`Fetching stats for project ${projectId}`);
        
        const response = await apiClient.get<ApiResponse<ProjectStatsResponse>>(`${this.baseUrl}/${projectId}/stats`);
        this.log.info(`Fetched stats for project ${projectId}: ${response.data.data.completionPercentage}% complete`);
        return response.data.data;
    }

    /**
     * Creates a duplicate of an existing project with a new name
     * @param projectId The unique identifier of the project to duplicate
     * @param newName The name for the duplicated project
     * @returns Promise resolving to the newly created duplicate project
     */
    async duplicateProject(projectId: number, newName: string): Promise<Project> {
        this.log.debug(`Duplicating project ${projectId} with name: ${newName}`);
        
        const response = await apiClient.post<ApiResponse<Project>>(`${this.baseUrl}/${projectId}/duplicate`, {
            name: newName
        });
        this.log.info(`Duplicated project: ${response.data.data.name} (ID: ${response.data.data.id})`);
        return response.data.data;
    }

    /**
     * Exports project data in the specified format
     * @param projectId The unique identifier of the project to export
     * @param format The export format (json, csv, or coco)
     * @returns Promise resolving to the exported data as a Blob
     */
    async exportProject(projectId: number, format: 'json' | 'csv' | 'coco'): Promise<Blob> {
        this.log.debug(`Exporting project ${projectId} in ${format} format`);
        
        const response = await apiClient.get(`${this.baseUrl}/${projectId}/export`, {
            params: { format },
            responseType: 'blob'
        });
        this.log.info(`Exported project ${projectId} in ${format} format`);
        return response.data;
    }
}

export const projectService = new ProjectService();
