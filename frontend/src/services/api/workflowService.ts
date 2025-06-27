import apiClient from "./apiClient";
import type { PaginatedResponse } from "@/types/api/paginatedResponse";
import type {
    Workflow,
    WorkflowStage,
    CreateWorkflowRequest,
    UpdateWorkflowRequest,
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

const logger = AppLogger.createServiceLogger('WorkflowService');

class WorkflowService {
    private readonly baseUrl = "/projects";

    // Workflow CRUD operations
    async getWorkflows(projectId: number): Promise<Workflow[]> {
        logger.info(`Fetching workflows for project: ${projectId}`);
        
        try {
            const response = await apiClient.get<PaginatedResponse<Workflow>>(`${this.baseUrl}/${projectId}/workflows`);
            
            if (isValidPaginatedResponse(response)) {
                logger.info(`Successfully fetched ${response.data.data.length} workflows for project ${projectId}`);
                return response.data.data;
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

    // Workflow Stage operations
    async getWorkflowStages(projectId: number, workflowId: number): Promise<WorkflowStage[]> {
        logger.info(`Fetching stages for workflow ${workflowId} in project ${projectId}`);
        
        try {
            const response = await apiClient.get<WorkflowStage[]>(`${this.baseUrl}/${projectId}/workflows/${workflowId}/stages`);
            
            if (isValidApiResponse(response, 'array')) {
                logger.info(`Successfully fetched ${response.data.length} stages for workflow ${workflowId}`);
                return response.data.sort((a, b) => a.stageOrder - b.stageOrder); // Ensure proper ordering
            } else {
                logger.error(`Invalid response structure when fetching stages for workflow ${workflowId}`, response?.data);
                throw new ApiResponseError('Invalid response structure from API when fetching workflow stages.');
            }
        } catch (error) {
            logger.error(`Failed to fetch stages for workflow ${workflowId}`, error);
            throw transformApiError(error, 'Failed to fetch workflow stages');
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

    // Combined operations for convenience
    async getWorkflowWithStages(projectId: number, workflowId: number): Promise<WorkflowWithStages> {
        const [workflow, stages] = await Promise.all([
            this.getWorkflow(projectId, workflowId),
            this.getWorkflowStages(projectId, workflowId)
        ]);
        
        return {
            ...workflow,
            stages
        };
    }
}

export const workflowService = new WorkflowService();
