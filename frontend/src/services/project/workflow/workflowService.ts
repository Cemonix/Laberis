import { BaseProjectService } from '../baseProjectService';
import type { PaginatedResponse } from "@/services/base/paginatedResponse";
import type {
    Workflow,
    CreateWorkflowRequest,
    UpdateWorkflowRequest,
    CreateWorkflowWithStagesRequest
} from "./workflow.types";
import type { QueryParams } from '@/services/base/requests';

/**
 * Service class for managing workflows within projects.
 * Extends BaseProjectService to inherit common project-related functionality.
 */
class WorkflowService extends BaseProjectService {
    constructor() {
        super('WorkflowService');
    }

    /**
     * Retrieves workflows for a project with optional filtering and pagination
     */
    async getWorkflows(
        projectId: number,
        options?: QueryParams
    ): Promise<PaginatedResponse<Workflow>> {
        this.logger.info(`Fetching workflows for project: ${projectId}`, options);
        
        const url = this.buildProjectUrl(projectId, 'workflows');
        const response = await this.getPaginated<Workflow>(url, options);
        
        this.logger.info(`Successfully fetched ${response.data.length} workflows for project ${projectId}`);
        return response;
    }

    /**
     * Retrieves a single workflow by its ID
     */
    async getWorkflowById(projectId: number, workflowId: number): Promise<Workflow> {
        this.logger.info(`Fetching workflow ${workflowId} from project ${projectId}`);
        
        const url = this.buildProjectUrl(projectId, `workflows/${workflowId}`);
        const response = await this.get<Workflow>(url);
        
        this.logger.info(`Successfully fetched workflow: ${response.name} (ID: ${workflowId})`);
        return response;
    }

    /**
     * Creates a new workflow within a project (supports both simple workflows and workflows with stages)
     */
    async createWorkflow(projectId: number, data: CreateWorkflowRequest | CreateWorkflowWithStagesRequest): Promise<Workflow> {
        this.logger.info(`Creating workflow in project ${projectId}`, data);
        
        const url = this.buildProjectUrl(projectId, 'workflows');
        const response = await this.post<CreateWorkflowRequest | CreateWorkflowWithStagesRequest, Workflow>(url, data);
        
        this.logger.info(`Created workflow: ${response.name} (ID: ${response.id}) in project ${projectId}`);
        return response;
    }


    /**
     * Updates an existing workflow
     */
    async updateWorkflow(
        projectId: number, 
        workflowId: number, 
        data: UpdateWorkflowRequest
    ): Promise<Workflow> {
        this.logger.info(`Updating workflow ${workflowId} in project ${projectId}`, data);
        
        const url = this.buildProjectUrl(projectId, `workflows/${workflowId}`);
        const response = await this.put<UpdateWorkflowRequest, Workflow>(url, data);
        
        this.logger.info(`Updated workflow: ${response.name} (ID: ${workflowId}) in project ${projectId}`);
        return response;
    }

    /**
     * Deletes a workflow from a project
     */
    async deleteWorkflow(projectId: number, workflowId: number): Promise<void> {
        this.logger.info(`Deleting workflow ${workflowId} from project ${projectId}`);
        
        const url = this.buildProjectUrl(projectId, `workflows/${workflowId}`);
        await this.delete(url);
        
        this.logger.info(`Successfully deleted workflow ${workflowId} from project ${projectId}`);
    }
}

export const workflowService = new WorkflowService();