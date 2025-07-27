import { BaseProjectService } from '../base';
import type { PaginatedResponse } from "@/types/api/paginatedResponse";
import type {
    WorkflowStage,
    CreateWorkflowStageRequest,
    UpdateWorkflowStageRequest,
    WorkflowWithStages
} from "@/types/workflow";
import type { QueryParams } from '@/types/api';

/**
 * Service class for managing workflow stages within projects.
 * Extends BaseProjectService to inherit common project-related functionality.
 */
class WorkflowStageService extends BaseProjectService {
    constructor() {
        super('WorkflowStageService');
    }

    /**
     * Retrieves stages for a specific workflow with optional filtering and pagination
     */
    async getWorkflowStages(
        projectId: number,
        workflowId: number,
        options?: QueryParams
    ): Promise<PaginatedResponse<WorkflowStage>> {
        this.logger.info(`Fetching stages for workflow ${workflowId} in project ${projectId}`, options);
        
        const url = this.buildProjectResourceUrl(projectId, 'workflows/{workflowId}/stages', { workflowId });
        const response = await this.getPaginated<WorkflowStage>(url, options);
        
        // Ensure proper ordering by stage order
        const sortedStages = response.data.sort((a, b) => a.stageOrder - b.stageOrder);

        this.logger.info(`Successfully fetched ${sortedStages.length} stages for workflow ${workflowId}`);
        return {
            ...response,
            data: sortedStages
        };
    }

    /**
     * Retrieves a single workflow stage by its ID
     */
    async getWorkflowStageById(
        projectId: number, 
        workflowId: number, 
        stageId: number
    ): Promise<WorkflowStage> {
        this.logger.info(`Fetching stage ${stageId} from workflow ${workflowId} in project ${projectId}`);
        
        const url = this.buildProjectResourceUrl(
            projectId, 
            'workflows/{workflowId}/stages/{stageId}', 
            { workflowId, stageId }
        );
        const response = await this.get<WorkflowStage>(url);
        
        this.logger.info(`Successfully fetched stage: ${response.name} (ID: ${stageId})`);
        return response;
    }

    /**
     * Creates a new workflow stage within a workflow
     */
    async createWorkflowStage(
        projectId: number, 
        workflowId: number, 
        data: CreateWorkflowStageRequest
    ): Promise<WorkflowStage> {
        this.logger.info(`Creating stage in workflow ${workflowId}, project ${projectId}`, data);
        
        const url = this.buildProjectResourceUrl(projectId, 'workflows/{workflowId}/stages', { workflowId });
        const response = await this.post<CreateWorkflowStageRequest, WorkflowStage>(url, data);
        
        this.logger.info(`Created stage: ${response.name} (ID: ${response.id}) in workflow ${workflowId}`);
        return response;
    }

    /**
     * Updates an existing workflow stage
     */
    async updateWorkflowStage(
        projectId: number, 
        workflowId: number, 
        stageId: number, 
        data: UpdateWorkflowStageRequest
    ): Promise<WorkflowStage> {
        this.logger.info(`Updating stage ${stageId} in workflow ${workflowId}, project ${projectId}`, data);
        
        const url = this.buildProjectResourceUrl(
            projectId, 
            'workflows/{workflowId}/stages/{stageId}', 
            { workflowId, stageId }
        );
        const response = await this.put<UpdateWorkflowStageRequest, WorkflowStage>(url, data);
        
        this.logger.info(`Updated stage: ${response.name} (ID: ${stageId}) in workflow ${workflowId}`);
        return response;
    }

    /**
     * Deletes a workflow stage
     */
    async deleteWorkflowStage(
        projectId: number, 
        workflowId: number, 
        stageId: number
    ): Promise<void> {
        this.logger.info(`Deleting stage ${stageId} from workflow ${workflowId} in project ${projectId}`);
        
        const url = this.buildProjectResourceUrl(
            projectId, 
            'workflows/{workflowId}/stages/{stageId}', 
            { workflowId, stageId }
        );
        await this.delete(url);
        
        this.logger.info(`Successfully deleted stage ${stageId} from workflow ${workflowId}`);
    }

    /**
     * Gets a workflow with all its stages
     */
    async getWorkflowWithStages(projectId: number, workflowId: number): Promise<WorkflowWithStages> {
        this.logger.info(`Fetching workflow ${workflowId} with stages from project ${projectId}`);
        
        const url = this.buildProjectResourceUrl(projectId, 'workflows/{workflowId}/with-stages', { workflowId });
        const response = await this.get<WorkflowWithStages>(url);
        
        this.logger.info(`Successfully fetched workflow with ${response.stages.length} stages`);
        return response;
    }

    /**
     * Reorders workflow stages
     */
    async reorderWorkflowStages(
        projectId: number, 
        workflowId: number, 
        stageOrders: { stageId: number; order: number }[]
    ): Promise<WorkflowStage[]> {
        this.logger.info(`Reordering stages in workflow ${workflowId}, project ${projectId}`, stageOrders);
        
        const url = this.buildProjectResourceUrl(projectId, 'workflows/{workflowId}/stages/reorder', { workflowId });
        const response = await this.post<{ stageOrders: typeof stageOrders }, WorkflowStage[]>(
            url, 
            { stageOrders }
        );
        
        this.logger.info(`Successfully reordered ${response.length} stages in workflow ${workflowId}`);
        return response;
    }
}

export const workflowStageService = new WorkflowStageService();