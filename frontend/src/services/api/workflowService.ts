import apiClient from "./apiClient";
import type { PaginatedResponse } from "@/types/api/paginatedResponse";
import type {
    Workflow,
    CreateWorkflowRequest,
    UpdateWorkflowRequest,
    CreateWorkflowWithStagesRequest
} from "@/types/workflow";
import { AppLogger } from "@/utils/logger";
import { ApiResponseError } from "@/types/common";
import {
    transformApiError,
    isValidApiResponse,
    isValidPaginatedResponse
} from "@/services/utils";

const logger = AppLogger.createServiceLogger('WorkflowService');

class WorkflowService {
    private readonly baseUrl = "/projects";

    // Workflow CRUD operations
    async getWorkflows(
        projectId: number,
        options?: {
            filterOn?: string;
            filterQuery?: string;
            sortBy?: string;
            isAscending?: boolean;
            pageNumber?: number;
            pageSize?: number;
        }
    ): Promise<PaginatedResponse<Workflow>> {
        logger.info(`Fetching workflows for project: ${projectId}`, options);
        
        try {
            const params = new URLSearchParams();
            if (options?.filterOn) params.append('filterOn', options.filterOn);
            if (options?.filterQuery) params.append('filterQuery', options.filterQuery);
            if (options?.sortBy) params.append('sortBy', options.sortBy);
            if (options?.isAscending !== undefined) params.append('isAscending', options.isAscending.toString());
            if (options?.pageNumber) params.append('pageNumber', options.pageNumber.toString());
            if (options?.pageSize) params.append('pageSize', options.pageSize.toString());

            const url = `${this.baseUrl}/${projectId}/workflows${params.toString() ? `?${params.toString()}` : ''}`;
            const response = await apiClient.get<PaginatedResponse<Workflow>>(url);
            
            if (isValidPaginatedResponse(response)) {
                logger.info(`Successfully fetched ${response.data.data.length} workflows for project ${projectId}`);
                return response.data;
            } else {
                logger.error(`Invalid response structure when fetching workflows for project ${projectId}`, response?.data);
                throw new ApiResponseError('Invalid response structure from API when fetching workflows.');
            }
        } catch (error) {
            logger.error(`Failed to fetch workflows for project ${projectId}`, error);
            throw transformApiError(error, 'Failed to fetch workflows');
        }
    }

    async getWorkflow(projectId: number, workflowId: number): Promise<Workflow> {
        logger.info(`Fetching workflow ${workflowId} for project ${projectId}`);
        
        try {
            const response = await apiClient.get<Workflow>(`${this.baseUrl}/${projectId}/workflows/${workflowId}`);
            
            if (isValidApiResponse(response)) {
                logger.info(`Successfully fetched workflow ${workflowId}`);
                return response.data;
            } else {
                logger.error(`Invalid response structure when fetching workflow ${workflowId}`, response?.data);
                throw new ApiResponseError('Invalid response structure from API when fetching workflow.');
            }
        } catch (error) {
            logger.error(`Failed to fetch workflow ${workflowId}`, error);
            throw transformApiError(error, 'Failed to fetch workflow');
        }
    }

    async createWorkflow(projectId: number, data: CreateWorkflowRequest): Promise<Workflow> {
        logger.info(`Creating workflow for project ${projectId}:`, data.name);
        
        try {
            const response = await apiClient.post<Workflow>(`${this.baseUrl}/${projectId}/workflows`, data);
            
            if (isValidApiResponse(response)) {
                logger.info(`Successfully created workflow: ${response.data.name} (ID: ${response.data.id})`);
                return response.data;
            } else {
                logger.error('Invalid response structure when creating workflow', response?.data);
                throw new ApiResponseError('Invalid response structure from API when creating workflow.');
            }
        } catch (error) {
            logger.error('Failed to create workflow', error);
            throw transformApiError(error, 'Failed to create workflow');
        }
    }

    async updateWorkflow(projectId: number, workflowId: number, data: UpdateWorkflowRequest): Promise<Workflow> {
        logger.info(`Updating workflow ${workflowId} for project ${projectId}`);
        
        try {
            const response = await apiClient.put<Workflow>(`${this.baseUrl}/${projectId}/workflows/${workflowId}`, data);
            
            if (isValidApiResponse(response)) {
                logger.info(`Successfully updated workflow ${workflowId}`);
                return response.data;
            } else {
                logger.error(`Invalid response structure when updating workflow ${workflowId}`, response?.data);
                throw new ApiResponseError('Invalid response structure from API when updating workflow.');
            }
        } catch (error) {
            logger.error(`Failed to update workflow ${workflowId}`, error);
            throw transformApiError(error, 'Failed to update workflow');
        }
    }

    async deleteWorkflow(projectId: number, workflowId: number): Promise<void> {
        logger.info(`Deleting workflow ${workflowId} for project ${projectId}`);
        
        try {
            await apiClient.delete(`${this.baseUrl}/${projectId}/workflows/${workflowId}`);
            logger.info(`Successfully deleted workflow ${workflowId}`);
        } catch (error) {
            logger.error(`Failed to delete workflow ${workflowId}`, error);
            throw transformApiError(error, 'Failed to delete workflow');
        }
    }

    async createWorkflowWithStages(projectId: number, data: CreateWorkflowWithStagesRequest): Promise<Workflow> {
        logger.info(`Creating workflow with stages for project ${projectId}:`, data.name);
        
        try {
            const response = await apiClient.post<Workflow>(`${this.baseUrl}/${projectId}/workflows`, data);
            
            if (isValidApiResponse(response)) {
                logger.info(`Successfully created workflow with stages: ${response.data.name} (ID: ${response.data.id})`);
                return response.data;
            } else {
                logger.error('Invalid response structure when creating workflow with stages', response?.data);
                throw new ApiResponseError('Invalid response structure from API when creating workflow with stages.');
            }
        } catch (error) {
            logger.error('Failed to create workflow with stages', error);
            throw transformApiError(error, 'Failed to create workflow with stages');
        }
    }
}

export const workflowService = new WorkflowService();
