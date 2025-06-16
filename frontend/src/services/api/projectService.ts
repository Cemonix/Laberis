import apiClient from './apiClient';
import type { PaginatedResponse } from '@/types/api/paginatedResponse';
import type { ApiError } from '@/types/api/error';
import type { Project } from '@/types/project/project';
import type { 
    CreateProjectRequest, 
    UpdateProjectRequest, 
    ProjectListParams,
    ProjectStatsResponse
} from '@/types/project/requests';
import { AppLogger } from '@/utils/logger';

const logger = AppLogger.createServiceLogger('ProjectService');

class ProjectService {
    private readonly baseUrl = '/projects';

    /**
     * Retrieves projects with optional filtering, sorting, and pagination
     * @param params Query parameters for filtering and pagination
     * @returns Promise resolving to paginated project list
     */
    async getProjects(params?: ProjectListParams): Promise<PaginatedResponse<Project>> {
        logger.info(`Fetching projects...`, params);
        try {
            const response = await apiClient.get<PaginatedResponse<Project>>(this.baseUrl, { params });
            if (response && response.data && Array.isArray(response.data.data)) {
                logger.info(`Fetched ${response.data.data.length} projects (total: ${response.data.totalItems}).`, response.data);
                return response.data;
            } else {
                logger.error(`Invalid response structure for projects.`, response);
                throw new Error('Invalid response structure from API for projects.');
            }
        } catch (error) {
            const apiError = error as ApiError;
            logger.error(`Failed to fetch projects.`, apiError.response?.data || apiError.message);
            throw apiError;
        }
    }

    /**
     * Retrieves a single project by its ID
     * @param projectId The unique identifier of the project
     * @returns Promise resolving to the project data
     */
    async getProject(projectId: number): Promise<Project> {
        logger.info(`Fetching project ${projectId}...`);
        try {
            const response = await apiClient.get<Project>(`${this.baseUrl}/${projectId}`);
            if (response && response.data && response.data.id) {
                logger.info(`Fetched project: ${response.data.name} (ID: ${response.data.id}).`, response.data);
                return response.data;
            } else {
                logger.error(`Invalid response structure for project ${projectId}.`, response);
                throw new Error('Invalid response structure from API for single project.');
            }
        } catch (error) {
            const apiError = error as ApiError;
            logger.error(`Failed to fetch project ${projectId}.`, apiError.response?.data || apiError.message);
            throw apiError;
        }
    }

    /**
     * Creates a new project
     * @param projectData The project data for creation
     * @returns Promise resolving to the created project
     */
    async createProject(projectData: CreateProjectRequest): Promise<Project> {
        logger.info(`Creating project...`, projectData);
        try {
            const response = await apiClient.post<Project>(this.baseUrl, projectData);
            if (response && response.data && response.data.id) {
                logger.info(`Created project: ${response.data.name} (ID: ${response.data.id}).`, response.data);
                return response.data;
            } else {
                logger.error(`Invalid response structure after creating project.`, response);
                throw new Error('Invalid response structure from API after creating project.');
            }
        } catch (error) {
            const apiError = error as ApiError;
            logger.error(`Failed to create project.`, apiError.response?.data || apiError.message);
            throw apiError;
        }
    }

    /**
     * Updates an existing project
     * @param projectId The unique identifier of the project to update
     * @param projectData The updated project data
     * @returns Promise resolving to the updated project
     */
    async updateProject(projectId: number, projectData: UpdateProjectRequest): Promise<Project> {
        logger.info(`Updating project ${projectId}...`, projectData);
        try {
            const response = await apiClient.put<Project>(`${this.baseUrl}/${projectId}`, projectData);
            if (response && response.data && response.data.id) {
                logger.info(`Updated project: ${response.data.name} (ID: ${projectId}).`, response.data);
                return response.data;
            } else {
                logger.error(`Invalid response structure after updating project ${projectId}.`, response);
                throw new Error('Invalid response structure from API after updating project.');
            }
        } catch (error) {
            const apiError = error as ApiError;
            logger.error(`Failed to update project ${projectId}.`, apiError.response?.data || apiError.message);
            throw apiError;
        }
    }

    /**
     * Permanently deletes a project and all associated data
     * @param projectId The unique identifier of the project to delete
     */
    async deleteProject(projectId: number): Promise<void> {
        logger.info(`Deleting project ${projectId}...`);
        try {
            const response = await apiClient.delete(`${this.baseUrl}/${projectId}`);
            if (response.status === 204 || response.status === 200) {
                logger.info(`Deleted project ${projectId} successfully.`);
            } else {
                logger.error(`Unexpected status code after deleting project ${projectId}.`, response);
                throw new Error('Unexpected response from API after deleting project.');
            }
        } catch (error) {
            const apiError = error as ApiError;
            logger.error(`Failed to delete project ${projectId}.`, apiError.response?.data || apiError.message);
            throw apiError;
        }
    }

    /**
     * Archives a project (soft delete - changes status to archived)
     * @param projectId The unique identifier of the project to archive
     * @returns Promise resolving to the archived project
     */
    async archiveProject(projectId: number): Promise<Project> {
        logger.info(`Archiving project ${projectId}...`);
        try {
            const response = await apiClient.patch<Project>(`${this.baseUrl}/${projectId}/archive`);
            if (response && response.data && response.data.id) {
                logger.info(`Archived project: ${response.data.name} (ID: ${projectId}).`, response.data);
                return response.data;
            } else {
                logger.error(`Invalid response structure after archiving project ${projectId}.`, response);
                throw new Error('Invalid response structure from API after archiving project.');
            }
        } catch (error) {
            const apiError = error as ApiError;
            logger.error(`Failed to archive project ${projectId}.`, apiError.response?.data || apiError.message);
            throw apiError;
        }
    }

    /**
     * Restores an archived project back to active status
     * @param projectId The unique identifier of the project to restore
     * @returns Promise resolving to the restored project
     */
    async restoreProject(projectId: number): Promise<Project> {
        logger.info(`Restoring project ${projectId}...`);
        try {
            const response = await apiClient.patch<Project>(`${this.baseUrl}/${projectId}/restore`);
            if (response && response.data && response.data.id) {
                logger.info(`Restored project: ${response.data.name} (ID: ${projectId}).`, response.data);
                return response.data;
            } else {
                logger.error(`Invalid response structure after restoring project ${projectId}.`, response);
                throw new Error('Invalid response structure from API after restoring project.');
            }
        } catch (error) {
            const apiError = error as ApiError;
            logger.error(`Failed to restore project ${projectId}.`, apiError.response?.data || apiError.message);
            throw apiError;
        }
    }

    /**
     * Retrieves detailed statistics and progress information for a project
     * @param projectId The unique identifier of the project
     * @returns Promise resolving to project statistics
     */
    async getProjectStats(projectId: number): Promise<ProjectStatsResponse> {
        logger.info(`Fetching stats for project ${projectId}...`);
        try {
            const response = await apiClient.get<ProjectStatsResponse>(`${this.baseUrl}/${projectId}/stats`);
            if (response && response.data) { 
                logger.info(`Fetched stats for project ${projectId}.`, response.data);
                return response.data;
            } else {
                logger.error(`Invalid response structure for project stats ${projectId}.`, response);
                throw new Error('Invalid response structure from API for project stats.');
            }
        } catch (error) {
            const apiError = error as ApiError;
            logger.error(`Failed to fetch stats for project ${projectId}.`, apiError.response?.data || apiError.message);
            throw apiError;
        }
    }

    /**
     * Creates a duplicate of an existing project with a new name
     * @param projectId The unique identifier of the project to duplicate
     * @param newName The name for the duplicated project
     * @returns Promise resolving to the newly created duplicate project
     */
    async duplicateProject(projectId: number, newName: string): Promise<Project> {
        logger.info(`Duplicating project ${projectId} with name: ${newName}...`);
        try {
            const response = await apiClient.post<Project>(`${this.baseUrl}/${projectId}/duplicate`, { name: newName });
            if (response && response.data && response.data.id) {
                logger.info(`Duplicated project: ${response.data.name} (ID: ${response.data.id}).`, response.data);
                return response.data;
            } else {
                logger.error(`Invalid response structure after duplicating project ${projectId}.`, response);
                throw new Error('Invalid response structure from API after duplicating project.');
            }
        } catch (error) {
            const apiError = error as ApiError;
            logger.error(`Failed to duplicate project ${projectId}.`, apiError.response?.data || apiError.message);
            throw apiError;
        }
    }

    /**
     * Exports project data in the specified format
     * @param projectId The unique identifier of the project to export
     * @param format The export format (json, csv, or coco)
     * @returns Promise resolving to the exported data as a Blob
     */
    async exportProject(projectId: number, format: 'json' | 'csv' | 'coco'): Promise<Blob> {
        logger.info(`Exporting project ${projectId} in ${format} format...`);
        try {
            const response = await apiClient.get<Blob>(`${this.baseUrl}/${projectId}/export`, {
                params: { format },
                responseType: 'blob'
            });
            if (response && response.data) { // Blob data might not have specific properties to check beyond existence
                logger.info(`Exported project ${projectId} in ${format} format successfully.`);
                return response.data;
            } else {
                logger.error(`Invalid response structure for project export ${projectId}.`, response);
                throw new Error('Invalid response structure from API for project export.');
            }
        } catch (error) {
            const apiError = error as ApiError;
            logger.error(`Failed to export project ${projectId} in ${format} format.`, apiError.response?.data || apiError.message);
            throw apiError;
        }
    }
}

export const projectService = new ProjectService();
