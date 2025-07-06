import apiClient from "./apiClient";
import type { PaginatedResponse } from "@/types/api/paginatedResponse";
import type {
    WorkflowStage,
    CreateWorkflowStageRequest,
    UpdateWorkflowStageRequest,
    WorkflowWithStages
} from "@/types/workflow";
import { AppLogger } from "@/utils/logger";
import { ApiResponseError } from "@/types/common";
import {
    transformApiError,
    isValidApiResponse,
    isValidPaginatedResponse
} from "@/services/utils";

const logger = AppLogger.createServiceLogger('WorkflowStageService');

class WorkflowStageService {
    private readonly baseUrl = "/projects";

    async getWorkflowStages(
        projectId: number,
        workflowId: number,
        options?: {
            filterOn?: string;
            filterQuery?: string;
            sortBy?: string;
            isAscending?: boolean;
            pageNumber?: number;
            pageSize?: number;
        }
    ): Promise<PaginatedResponse<WorkflowStage>> {
        logger.info(`Fetching stages for workflow ${workflowId} in project ${projectId}`, options);
        
        try {
            const params = new URLSearchParams();
            if (options?.filterOn) params.append('filterOn', options.filterOn);
            if (options?.filterQuery) params.append('filterQuery', options.filterQuery);
            if (options?.sortBy) params.append('sortBy', options.sortBy);
            if (options?.isAscending !== undefined) params.append('isAscending', options.isAscending.toString());
            if (options?.pageNumber) params.append('pageNumber', options.pageNumber.toString());
            if (options?.pageSize) params.append('pageSize', options.pageSize.toString());

            const url = `${this.baseUrl}/${projectId}/workflows/${workflowId}/stages${params.toString() ? `?${params.toString()}` : ''}`;
            const response = await apiClient.get<PaginatedResponse<WorkflowStage>>(url);
            
            if (isValidPaginatedResponse(response)) {
                logger.info(`Successfully fetched ${response.data.data.length} stages for workflow ${workflowId}`);
                // Ensure proper ordering
                response.data.data.sort((a, b) => a.stageOrder - b.stageOrder);
                return response.data;
            } else {
                logger.error(`Invalid response structure when fetching stages for workflow ${workflowId}`, response?.data);
                throw new ApiResponseError('Invalid response structure from API when fetching workflow stages.');
            }
        } catch (error) {
            logger.error(`Failed to fetch stages for workflow ${workflowId}`, error);
            throw transformApiError(error, 'Failed to fetch workflow stages');
        }
    }

    async getWorkflowStage(projectId: number, workflowId: number, stageId: number): Promise<WorkflowStage> {
        logger.info(`Fetching stage ${stageId} for workflow ${workflowId} in project ${projectId}`);
        
        try {
            const response = await apiClient.get<WorkflowStage>(`${this.baseUrl}/${projectId}/workflows/${workflowId}/stages/${stageId}`);
            
            if (isValidApiResponse(response)) {
                logger.info(`Successfully fetched stage ${stageId}`);
                return response.data;
            } else {
                logger.error(`Invalid response structure when fetching stage ${stageId}`, response?.data);
                throw new ApiResponseError('Invalid response structure from API when fetching workflow stage.');
            }
        } catch (error) {
            logger.error(`Failed to fetch stage ${stageId}`, error);
            throw transformApiError(error, 'Failed to fetch workflow stage');
        }
    }

    async createWorkflowStage(projectId: number, workflowId: number, data: CreateWorkflowStageRequest): Promise<WorkflowStage> {
        logger.info(`Creating stage for workflow ${workflowId}:`, data.name);
        
        try {
            const response = await apiClient.post<WorkflowStage>(`${this.baseUrl}/${projectId}/workflows/${workflowId}/stages`, data);
            
            if (isValidApiResponse(response)) {
                logger.info(`Successfully created stage: ${response.data.name} (ID: ${response.data.id})`);
                return response.data;
            } else {
                logger.error('Invalid response structure when creating workflow stage', response?.data);
                throw new ApiResponseError('Invalid response structure from API when creating workflow stage.');
            }
        } catch (error) {
            logger.error('Failed to create workflow stage', error);
            throw transformApiError(error, 'Failed to create workflow stage');
        }
    }

    async updateWorkflowStage(projectId: number, workflowId: number, stageId: number, data: UpdateWorkflowStageRequest): Promise<WorkflowStage> {
        logger.info(`Updating stage ${stageId} for workflow ${workflowId}`);
        
        try {
            const response = await apiClient.put<WorkflowStage>(`${this.baseUrl}/${projectId}/workflows/${workflowId}/stages/${stageId}`, data);
            
            if (isValidApiResponse(response)) {
                logger.info(`Successfully updated stage ${stageId}`);
                return response.data;
            } else {
                logger.error(`Invalid response structure when updating stage ${stageId}`, response?.data);
                throw new ApiResponseError('Invalid response structure from API when updating workflow stage.');
            }
        } catch (error) {
            logger.error(`Failed to update stage ${stageId}`, error);
            throw transformApiError(error, 'Failed to update workflow stage');
        }
    }

    async deleteWorkflowStage(projectId: number, workflowId: number, stageId: number): Promise<void> {
        logger.info(`Deleting stage ${stageId} for workflow ${workflowId}`);
        
        try {
            await apiClient.delete(`${this.baseUrl}/${projectId}/workflows/${workflowId}/stages/${stageId}`);
            logger.info(`Successfully deleted stage ${stageId}`);
        } catch (error) {
            logger.error(`Failed to delete stage ${stageId}`, error);
            throw transformApiError(error, 'Failed to delete workflow stage');
        }
    }

    async reorderWorkflowStages(projectId: number, workflowId: number, stageIds: number[]): Promise<void> {
        logger.info(`Reordering stages for workflow ${workflowId}`, stageIds);
        
        try {
            await apiClient.put(`${this.baseUrl}/${projectId}/workflows/${workflowId}/stages/reorder`, stageIds);
            logger.info(`Successfully reordered stages for workflow ${workflowId}`);
        } catch (error) {
            logger.error(`Failed to reorder stages for workflow ${workflowId}`, error);
            throw transformApiError(error, 'Failed to reorder workflow stages');
        }
    }

    // Pipeline visualization - get all stages with connections
    async getWorkflowStagesForPipeline(projectId: number, workflowId: number): Promise<WorkflowStage[]> {
        logger.info(`Fetching pipeline stages for workflow ${workflowId} in project ${projectId}`);
        
        try {
            const response = await apiClient.get<WorkflowStage[]>(`${this.baseUrl}/${projectId}/workflows/${workflowId}/stages/pipeline`);
            
            if (isValidApiResponse(response, 'array')) {
                logger.info(`Successfully fetched ${response.data.length} pipeline stages for workflow ${workflowId}`);
                // Ensure proper ordering for pipeline visualization
                return response.data.sort((a, b) => a.stageOrder - b.stageOrder);
            } else {
                logger.error(`Invalid response structure when fetching pipeline stages for workflow ${workflowId}`, response?.data);
                throw new ApiResponseError('Invalid response structure from API when fetching pipeline stages.');
            }
        } catch (error) {
            logger.error(`Failed to fetch pipeline stages for workflow ${workflowId}`, error);
            throw transformApiError(error, 'Failed to fetch pipeline stages');
        }
    }

    // Combined operations for convenience
    // TODO: Fix this poorly structured method
    async getWorkflowWithStages(projectId: number, workflowId: number): Promise<WorkflowWithStages> {
        logger.info(`Fetching workflow ${workflowId} with stages for project ${projectId}`);
        
        try {
            // Import workflow service here to avoid circular dependency
            const { workflowService } = await import('./workflowService');
            
            const [workflow, stages] = await Promise.all([
                workflowService.getWorkflow(projectId, workflowId),
                this.getWorkflowStagesForPipeline(projectId, workflowId) // Use pipeline method for connections and assignments
            ]);
            
            return {
                ...workflow,
                stages: stages
            };
        } catch (error) {
            logger.error(`Failed to fetch workflow ${workflowId} with stages`, error);
            throw transformApiError(error, 'Failed to fetch workflow with stages');
        }
    }
}

export const workflowStageService = new WorkflowStageService();
