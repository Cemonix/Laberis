import { BaseProjectService } from '../baseProjectService';
import type {
    WorkflowStageConnection,
    CreateWorkflowStageConnectionRequest,
    UpdateWorkflowStageConnectionRequest
} from "./workflowStage.types";

/**
 * Service class for managing workflow stage connections within projects.
 * Extends BaseProjectService to inherit common project-related functionality.
 */
class WorkflowStageConnectionService extends BaseProjectService {
    constructor() {
        super('WorkflowStageConnectionService');
    }

    /**
     * Retrieves all connections for a specific workflow
     */
    async getWorkflowConnections(projectId: number, workflowId: number): Promise<WorkflowStageConnection[]> {
        this.logger.info(`Fetching connections for workflow ${workflowId} in project ${projectId}`);
        
        const url = this.buildProjectResourceUrl(projectId, 'workflows/{workflowId}/connections', { workflowId });
        const response = await this.get<WorkflowStageConnection[]>(url);
        
        this.logger.info(`Successfully fetched ${response.length} connections for workflow ${workflowId}`);
        return response;
    }

    /**
     * Retrieves a single workflow stage connection by its ID
     */
    async getConnection(projectId: number, workflowId: number, connectionId: number): Promise<WorkflowStageConnection> {
        this.logger.info(`Fetching connection ${connectionId} for workflow ${workflowId} in project ${projectId}`);
        
        const url = this.buildProjectResourceUrl(
            projectId, 
            'workflows/{workflowId}/connections/{connectionId}', 
            { workflowId, connectionId }
        );
        const response = await this.get<WorkflowStageConnection>(url);
        
        this.logger.info(`Successfully fetched connection ${connectionId}`);
        return response;
    }

    /**
     * Creates a new workflow stage connection
     */
    async createConnection(
        projectId: number, 
        workflowId: number, 
        data: CreateWorkflowStageConnectionRequest
    ): Promise<WorkflowStageConnection> {
        this.logger.info(`Creating connection in workflow ${workflowId}, project ${projectId}`, data);
        
        const url = this.buildProjectResourceUrl(projectId, 'workflows/{workflowId}/connections', { workflowId });
        const response = await this.post<CreateWorkflowStageConnectionRequest, WorkflowStageConnection>(url, data);
        
        this.logger.info(`Successfully created connection ${response.id} in workflow ${workflowId}`);
        return response;
    }

    /**
     * Updates an existing workflow stage connection
     */
    async updateConnection(
        projectId: number, 
        workflowId: number, 
        connectionId: number, 
        data: UpdateWorkflowStageConnectionRequest
    ): Promise<WorkflowStageConnection> {
        this.logger.info(`Updating connection ${connectionId} in workflow ${workflowId}, project ${projectId}`, data);
        
        const url = this.buildProjectResourceUrl(
            projectId, 
            'workflows/{workflowId}/connections/{connectionId}', 
            { workflowId, connectionId }
        );
        const response = await this.put<UpdateWorkflowStageConnectionRequest, WorkflowStageConnection>(url, data);
        
        this.logger.info(`Successfully updated connection ${connectionId}`);
        return response;
    }

    /**
     * Deletes a workflow stage connection
     */
    async deleteConnection(
        projectId: number, 
        workflowId: number, 
        connectionId: number
    ): Promise<void> {
        this.logger.info(`Deleting connection ${connectionId} from workflow ${workflowId} in project ${projectId}`);
        
        const url = this.buildProjectResourceUrl(
            projectId, 
            'workflows/{workflowId}/connections/{connectionId}', 
            { workflowId, connectionId }
        );
        await this.delete(url);
        
        this.logger.info(`Successfully deleted connection ${connectionId}`);
    }
}

export const workflowStageConnectionService = new WorkflowStageConnectionService();