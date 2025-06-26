import apiClient from "./apiClient";
import type { ApiError } from "@/types/api/error";
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

const logger = AppLogger.createServiceLogger('WorkflowService');

class WorkflowService {
    private readonly baseUrl = "/projects";

    // Workflow CRUD operations
    async getWorkflows(projectId: number): Promise<Workflow[]> {
        logger.info(`Fetching workflows for project: ${projectId}`);
        
        try {
            const response = await apiClient.get<PaginatedResponse<Workflow>>(`${this.baseUrl}/${projectId}/workflows`);
            
            if (response?.data) {
                logger.info(`Successfully fetched ${response.data.data.length} workflows for project ${projectId}`);
                return response.data.data;
            } else {
                logger.error(`Invalid response structure when fetching workflows for project ${projectId}`);
                throw new Error('Invalid response structure from API when fetching workflows.');
            }
        } catch (error) {
            const apiError = error as ApiError;
            logger.error(`Failed to fetch workflows for project ${projectId}`, apiError.response?.data || apiError.message);

            if (apiError.response?.status === 404) {
                throw new Error('Project not found');
            } else if (apiError.response?.status === 403) {
                throw new Error('Access denied. You do not have permission to view workflows for this project.');
            }
            
            throw new Error('Failed to fetch workflows. Please try again later.');
        }
    }

    async getWorkflow(projectId: number, workflowId: number): Promise<Workflow> {
        logger.info(`Fetching workflow ${workflowId} for project ${projectId}`);
        
        try {
            const response = await apiClient.get<Workflow>(`${this.baseUrl}/${projectId}/workflows/${workflowId}`);
            
            if (response?.data) {
                logger.info(`Successfully fetched workflow ${workflowId}`);
                return response.data;
            } else {
                logger.error(`Invalid response structure when fetching workflow ${workflowId}`);
                throw new Error('Invalid response structure from API when fetching workflow.');
            }
        } catch (error) {
            const apiError = error as ApiError;
            logger.error(`Failed to fetch workflow ${workflowId}`, apiError.response?.data || apiError.message);
            
            if (apiError.response?.status === 404) {
                throw new Error('Workflow not found');
            } else if (apiError.response?.status === 403) {
                throw new Error('Access denied. You do not have permission to view this workflow.');
            }
            
            throw new Error('Failed to fetch workflow. Please try again later.');
        }
    }

    async createWorkflow(projectId: number, data: CreateWorkflowRequest): Promise<Workflow> {
        logger.info(`Creating workflow for project ${projectId}:`, data.name);
        
        try {
            const response = await apiClient.post<Workflow>(`${this.baseUrl}/${projectId}/workflows`, data);
            
            if (response?.data) {
                logger.info(`Successfully created workflow: ${response.data.name} (ID: ${response.data.id})`);
                return response.data;
            } else {
                logger.error('Invalid response structure when creating workflow');
                throw new Error('Invalid response structure from API when creating workflow.');
            }
        } catch (error) {
            const apiError = error as ApiError;
            logger.error('Failed to create workflow', apiError.response?.data || apiError.message);
            
            if (apiError.response?.status === 400) {
                const validationErrors = apiError.response.data?.errors;
                if (validationErrors) {
                    const errorMessages = Object.values(validationErrors).flat().join(', ');
                    throw new Error(errorMessages);
                }
                throw new Error('Invalid workflow data. Please check your input.');
            } else if (apiError.response?.status === 403) {
                throw new Error('Access denied. You do not have permission to create workflows for this project.');
            } else if (apiError.response?.status === 409) {
                throw new Error('A workflow with this name already exists in the project.');
            }
            
            throw new Error('Failed to create workflow. Please try again later.');
        }
    }

    async updateWorkflow(projectId: number, workflowId: number, data: UpdateWorkflowRequest): Promise<Workflow> {
        logger.info(`Updating workflow ${workflowId} for project ${projectId}`);
        
        try {
            const response = await apiClient.put<Workflow>(`${this.baseUrl}/${projectId}/workflows/${workflowId}`, data);
            
            if (response?.data) {
                logger.info(`Successfully updated workflow ${workflowId}`);
                return response.data;
            } else {
                logger.error(`Invalid response structure when updating workflow ${workflowId}`);
                throw new Error('Invalid response structure from API when updating workflow.');
            }
        } catch (error) {
            const apiError = error as ApiError;
            logger.error(`Failed to update workflow ${workflowId}`, apiError.response?.data || apiError.message);
            
            if (apiError.response?.status === 400) {
                const validationErrors = apiError.response.data?.errors;
                if (validationErrors) {
                    const errorMessages = Object.values(validationErrors).flat().join(', ');
                    throw new Error(errorMessages);
                }
                throw new Error('Invalid workflow data. Please check your input.');
            } else if (apiError.response?.status === 404) {
                throw new Error('Workflow not found');
            } else if (apiError.response?.status === 403) {
                throw new Error('Access denied. You do not have permission to update this workflow.');
            }
            
            throw new Error('Failed to update workflow. Please try again later.');
        }
    }

    async deleteWorkflow(projectId: number, workflowId: number): Promise<void> {
        logger.info(`Deleting workflow ${workflowId} for project ${projectId}`);
        
        try {
            await apiClient.delete(`${this.baseUrl}/${projectId}/workflows/${workflowId}`);
            logger.info(`Successfully deleted workflow ${workflowId}`);
        } catch (error) {
            const apiError = error as ApiError;
            logger.error(`Failed to delete workflow ${workflowId}`, apiError.response?.data || apiError.message);
            
            if (apiError.response?.status === 404) {
                throw new Error('Workflow not found');
            } else if (apiError.response?.status === 403) {
                throw new Error('Access denied. You do not have permission to delete this workflow.');
            } else if (apiError.response?.status === 409) {
                throw new Error('Cannot delete workflow. It is currently being used by active tasks.');
            }
            
            throw new Error('Failed to delete workflow. Please try again later.');
        }
    }

    // Workflow Stage operations
    async getWorkflowStages(projectId: number, workflowId: number): Promise<WorkflowStage[]> {
        logger.info(`Fetching stages for workflow ${workflowId} in project ${projectId}`);
        
        try {
            const response = await apiClient.get<WorkflowStage[]>(`${this.baseUrl}/${projectId}/workflows/${workflowId}/stages`);
            
            if (response?.data) {
                logger.info(`Successfully fetched ${response.data.length} stages for workflow ${workflowId}`);
                return response.data.sort((a, b) => a.stageOrder - b.stageOrder); // Ensure proper ordering
            } else {
                logger.error(`Invalid response structure when fetching stages for workflow ${workflowId}`);
                throw new Error('Invalid response structure from API when fetching workflow stages.');
            }
        } catch (error) {
            const apiError = error as ApiError;
            logger.error(`Failed to fetch stages for workflow ${workflowId}`, apiError.response?.data || apiError.message);
            
            if (apiError.response?.status === 404) {
                throw new Error('Workflow not found');
            } else if (apiError.response?.status === 403) {
                throw new Error('Access denied. You do not have permission to view stages for this workflow.');
            }
            
            throw new Error('Failed to fetch workflow stages. Please try again later.');
        }
    }

    async createWorkflowStage(projectId: number, workflowId: number, data: CreateWorkflowStageRequest): Promise<WorkflowStage> {
        logger.info(`Creating stage for workflow ${workflowId}:`, data.name);
        
        try {
            const response = await apiClient.post<WorkflowStage>(`${this.baseUrl}/${projectId}/workflows/${workflowId}/stages`, data);
            
            if (response?.data) {
                logger.info(`Successfully created stage: ${response.data.name} (ID: ${response.data.id})`);
                return response.data;
            } else {
                logger.error('Invalid response structure when creating workflow stage');
                throw new Error('Invalid response structure from API when creating workflow stage.');
            }
        } catch (error) {
            const apiError = error as ApiError;
            logger.error('Failed to create workflow stage', apiError.response?.data || apiError.message);
            
            if (apiError.response?.status === 400) {
                const validationErrors = apiError.response.data?.errors;
                if (validationErrors) {
                    const errorMessages = Object.values(validationErrors).flat().join(', ');
                    throw new Error(errorMessages);
                }
                throw new Error('Invalid stage data. Please check your input.');
            } else if (apiError.response?.status === 404) {
                throw new Error('Workflow not found');
            } else if (apiError.response?.status === 403) {
                throw new Error('Access denied. You do not have permission to create stages for this workflow.');
            }
            
            throw new Error('Failed to create workflow stage. Please try again later.');
        }
    }

    async updateWorkflowStage(projectId: number, workflowId: number, stageId: number, data: UpdateWorkflowStageRequest): Promise<WorkflowStage> {
        logger.info(`Updating stage ${stageId} for workflow ${workflowId}`);
        
        try {
            const response = await apiClient.put<WorkflowStage>(`${this.baseUrl}/${projectId}/workflows/${workflowId}/stages/${stageId}`, data);
            
            if (response?.data) {
                logger.info(`Successfully updated stage ${stageId}`);
                return response.data;
            } else {
                logger.error(`Invalid response structure when updating stage ${stageId}`);
                throw new Error('Invalid response structure from API when updating workflow stage.');
            }
        } catch (error) {
            const apiError = error as ApiError;
            logger.error(`Failed to update stage ${stageId}`, apiError.response?.data || apiError.message);
            
            if (apiError.response?.status === 400) {
                const validationErrors = apiError.response.data?.errors;
                if (validationErrors) {
                    const errorMessages = Object.values(validationErrors).flat().join(', ');
                    throw new Error(errorMessages);
                }
                throw new Error('Invalid stage data. Please check your input.');
            } else if (apiError.response?.status === 404) {
                throw new Error('Workflow stage not found');
            } else if (apiError.response?.status === 403) {
                throw new Error('Access denied. You do not have permission to update this workflow stage.');
            }
            
            throw new Error('Failed to update workflow stage. Please try again later.');
        }
    }

    async deleteWorkflowStage(projectId: number, workflowId: number, stageId: number): Promise<void> {
        logger.info(`Deleting stage ${stageId} for workflow ${workflowId}`);
        
        try {
            await apiClient.delete(`${this.baseUrl}/${projectId}/workflows/${workflowId}/stages/${stageId}`);
            logger.info(`Successfully deleted stage ${stageId}`);
        } catch (error) {
            const apiError = error as ApiError;
            logger.error(`Failed to delete stage ${stageId}`, apiError.response?.data || apiError.message);
            
            if (apiError.response?.status === 404) {
                throw new Error('Workflow stage not found');
            } else if (apiError.response?.status === 403) {
                throw new Error('Access denied. You do not have permission to delete this workflow stage.');
            } else if (apiError.response?.status === 409) {
                throw new Error('Cannot delete stage. It is currently being used by active tasks.');
            }
            
            throw new Error('Failed to delete workflow stage. Please try again later.');
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
