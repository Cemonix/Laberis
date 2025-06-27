import apiClient from './apiClient';
import type { PaginatedResponse } from '@/types/api/paginatedResponse';
import type { Project } from '@/types/project/project';
import type { 
    CreateProjectRequest, 
    UpdateProjectRequest,
    ProjectStatsResponse
} from '@/types/project/requests';
import type { QueryParams } from '@/types/api';
import { AppLogger } from '@/utils/logger';
import { ApiResponseError } from '@/types/common/errors';
import {
    transformApiError,
    isValidApiResponse,
    isValidPaginatedResponse,
    isValidBlobResponse
} from '@/services/utils';

const logger = AppLogger.createServiceLogger('ProjectService');

class ProjectService {
    private readonly baseUrl = '/projects';

    /**
     * Retrieves projects with optional filtering, sorting, and pagination
     * @param params Query parameters for filtering and pagination
     * @returns Promise resolving to paginated project list
     */
    async getProjects(params?: QueryParams): Promise<PaginatedResponse<Project>> {
        logger.info(`Fetching projects...`, params);
        try {
            const response = await apiClient.get<PaginatedResponse<Project>>(this.baseUrl, { params });
            if (isValidPaginatedResponse(response)) {
                logger.info(`Fetched ${response.data.data.length} projects (total: ${response.data.totalItems}).`, response.data);
                return response.data;
            } else {
                logger.error(`Invalid response structure for projects.`, response?.data);
                throw new ApiResponseError('Invalid response structure from API for projects.');
            }
        } catch (error) {
            logger.error(`Failed to fetch projects.`, error);
            throw transformApiError(error, 'Failed to fetch projects');
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
            if (isValidApiResponse(response)) {
                logger.info(`Fetched project: ${response.data.name} (ID: ${response.data.id}).`, response.data);
                return response.data;
            } else {
                logger.error(`Invalid response structure for project ${projectId}.`, response?.data);
                throw new ApiResponseError('Invalid response structure from API for single project.');
            }
        } catch (error) {
            logger.error(`Failed to fetch project ${projectId}.`, error);
            throw transformApiError(error, `Failed to fetch project ${projectId}`);
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
            if (isValidApiResponse(response)) {
                logger.info(`Created project: ${response.data.name} (ID: ${response.data.id}).`, response.data);
                return response.data;
            } else {
                logger.error(`Invalid response structure after creating project.`, response?.data);
                throw new ApiResponseError('Invalid response structure from API after creating project.');
            }
        } catch (error) {
            logger.error(`Failed to create project.`, error);
            throw transformApiError(error, 'Failed to create project');
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
            if (isValidApiResponse(response)) {
                logger.info(`Updated project: ${response.data.name} (ID: ${projectId}).`, response.data);
                return response.data;
            } else {
                logger.error(`Invalid response structure after updating project ${projectId}.`, response?.data);
                throw new ApiResponseError('Invalid response structure from API after updating project.');
            }
        } catch (error) {
            logger.error(`Failed to update project ${projectId}.`, error);
            throw transformApiError(error, `Failed to update project ${projectId}`);
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
                throw new ApiResponseError('Unexpected response from API after deleting project.');
            }
        } catch (error) {
            logger.error(`Failed to delete project ${projectId}.`, error);
            throw transformApiError(error, `Failed to delete project ${projectId}`);
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
            if (isValidApiResponse(response)) {
                logger.info(`Archived project: ${response.data.name} (ID: ${projectId}).`, response.data);
                return response.data;
            } else {
                logger.error(`Invalid response structure after archiving project ${projectId}.`, response?.data);
                throw new ApiResponseError('Invalid response structure from API after archiving project.');
            }
        } catch (error) {
            logger.error(`Failed to archive project ${projectId}.`, error);
            throw transformApiError(error, `Failed to archive project ${projectId}`);
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
            if (isValidApiResponse(response)) {
                logger.info(`Restored project: ${response.data.name} (ID: ${projectId}).`, response.data);
                return response.data;
            } else {
                logger.error(`Invalid response structure after restoring project ${projectId}.`, response?.data);
                throw new ApiResponseError('Invalid response structure from API after restoring project.');
            }
        } catch (error) {
            logger.error(`Failed to restore project ${projectId}.`, error);
            throw transformApiError(error, `Failed to restore project ${projectId}`);
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
            if (isValidApiResponse(response)) { 
                logger.info(`Fetched stats for project ${projectId}.`, response.data);
                return response.data;
            } else {
                logger.error(`Invalid response structure for project stats ${projectId}.`, response?.data);
                throw new ApiResponseError('Invalid response structure from API for project stats.');
            }
        } catch (error) {
            logger.error(`Failed to fetch stats for project ${projectId}.`, error);
            throw transformApiError(error, `Failed to fetch stats for project ${projectId}`);
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
            if (isValidApiResponse(response)) {
                logger.info(`Duplicated project: ${response.data.name} (ID: ${response.data.id}).`, response.data);
                return response.data;
            } else {
                logger.error(`Invalid response structure after duplicating project ${projectId}.`, response?.data);
                throw new ApiResponseError('Invalid response structure from API after duplicating project.');
            }
        } catch (error) {
            logger.error(`Failed to duplicate project ${projectId}.`, error);
            throw transformApiError(error, `Failed to duplicate project ${projectId}`);
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
            if (isValidBlobResponse(response)) {
                logger.info(`Exported project ${projectId} in ${format} format successfully.`);
                return response.data;
            } else {
                logger.error(`Invalid response structure for project export ${projectId}.`, response?.data);
                throw new ApiResponseError('Invalid response structure from API for project export.');
            }
        } catch (error) {
            logger.error(`Failed to export project ${projectId} in ${format} format.`, error);
            throw transformApiError(error, `Failed to export project ${projectId} in ${format} format`);
        }
    }
}

export const projectService = new ProjectService();
