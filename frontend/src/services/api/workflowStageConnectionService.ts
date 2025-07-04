import apiClient from "./apiClient";
import type {
    WorkflowStageConnection,
    CreateWorkflowStageConnectionRequest,
    UpdateWorkflowStageConnectionRequest
} from "@/types/workflow";
import { AppLogger } from "@/utils/logger";
import { ApiResponseError } from "@/types/common";
import {
    transformApiError,
    isValidApiResponse
} from "@/services/utils";

const logger = AppLogger.createServiceLogger('WorkflowStageConnectionService');

class WorkflowStageConnectionService {
    private readonly baseUrl = "/projects";

    // Workflow Stage Connection CRUD operations
    async getWorkflowConnections(projectId: number, workflowId: number): Promise<WorkflowStageConnection[]> {
        logger.info(`Fetching connections for workflow ${workflowId} in project ${projectId}`);
        
        try {
            const response = await apiClient.get<WorkflowStageConnection[]>(`${this.baseUrl}/${projectId}/workflows/${workflowId}/connections`);
            
            if (isValidApiResponse(response, 'array')) {
                logger.info(`Successfully fetched ${response.data.length} connections for workflow ${workflowId}`);
                return response.data;
            } else {
                logger.error(`Invalid response structure when fetching connections for workflow ${workflowId}`, response?.data);
                throw new ApiResponseError('Invalid response structure from API when fetching workflow connections.');
            }
        } catch (error) {
            logger.error(`Failed to fetch connections for workflow ${workflowId}`, error);
            throw transformApiError(error, 'Failed to fetch workflow connections');
        }
    }

    async getConnection(projectId: number, workflowId: number, connectionId: number): Promise<WorkflowStageConnection> {
        logger.info(`Fetching connection ${connectionId} for workflow ${workflowId} in project ${projectId}`);
        
        try {
            const response = await apiClient.get<WorkflowStageConnection>(`${this.baseUrl}/${projectId}/workflows/${workflowId}/connections/${connectionId}`);
            
            if (isValidApiResponse(response)) {
                logger.info(`Successfully fetched connection ${connectionId}`);
                return response.data;
            } else {
                logger.error(`Invalid response structure when fetching connection ${connectionId}`, response?.data);
                throw new ApiResponseError('Invalid response structure from API when fetching workflow connection.');
            }
        } catch (error) {
            logger.error(`Failed to fetch connection ${connectionId}`, error);
            throw transformApiError(error, 'Failed to fetch workflow connection');
        }
    }

    async createConnection(projectId: number, workflowId: number, data: CreateWorkflowStageConnectionRequest): Promise<WorkflowStageConnection> {
        logger.info(`Creating connection for workflow ${workflowId}:`, { fromStageId: data.fromStageId, toStageId: data.toStageId });
        
        try {
            const response = await apiClient.post<WorkflowStageConnection>(`${this.baseUrl}/${projectId}/workflows/${workflowId}/connections`, data);
            
            if (isValidApiResponse(response)) {
                logger.info(`Successfully created connection from stage ${data.fromStageId} to stage ${data.toStageId} (ID: ${response.data.id})`);
                return response.data;
            } else {
                logger.error('Invalid response structure when creating workflow connection', response?.data);
                throw new ApiResponseError('Invalid response structure from API when creating workflow connection.');
            }
        } catch (error) {
            logger.error('Failed to create workflow connection', error);
            throw transformApiError(error, 'Failed to create workflow connection');
        }
    }

    async updateConnection(projectId: number, workflowId: number, connectionId: number, data: UpdateWorkflowStageConnectionRequest): Promise<WorkflowStageConnection> {
        logger.info(`Updating connection ${connectionId} for workflow ${workflowId}`);
        
        try {
            const response = await apiClient.put<WorkflowStageConnection>(`${this.baseUrl}/${projectId}/workflows/${workflowId}/connections/${connectionId}`, data);
            
            if (isValidApiResponse(response)) {
                logger.info(`Successfully updated connection ${connectionId}`);
                return response.data;
            } else {
                logger.error(`Invalid response structure when updating connection ${connectionId}`, response?.data);
                throw new ApiResponseError('Invalid response structure from API when updating workflow connection.');
            }
        } catch (error) {
            logger.error(`Failed to update connection ${connectionId}`, error);
            throw transformApiError(error, 'Failed to update workflow connection');
        }
    }

    async deleteConnection(projectId: number, workflowId: number, connectionId: number): Promise<void> {
        logger.info(`Deleting connection ${connectionId} for workflow ${workflowId}`);
        
        try {
            await apiClient.delete(`${this.baseUrl}/${projectId}/workflows/${workflowId}/connections/${connectionId}`);
            logger.info(`Successfully deleted connection ${connectionId}`);
        } catch (error) {
            logger.error(`Failed to delete connection ${connectionId}`, error);
            throw transformApiError(error, 'Failed to delete workflow connection');
        }
    }

    // Utility methods for connection management
    async createConnectionsBatch(projectId: number, workflowId: number, connections: CreateWorkflowStageConnectionRequest[]): Promise<WorkflowStageConnection[]> {
        logger.info(`Creating ${connections.length} connections for workflow ${workflowId}`);
        
        try {
            const createdConnections = await Promise.all(
                connections.map(connection => this.createConnection(projectId, workflowId, connection))
            );
            
            logger.info(`Successfully created ${createdConnections.length} connections for workflow ${workflowId}`);
            return createdConnections;
        } catch (error) {
            logger.error(`Failed to create batch connections for workflow ${workflowId}`, error);
            throw transformApiError(error, 'Failed to create batch connections');
        }
    }

    async deleteConnectionsBatch(projectId: number, workflowId: number, connectionIds: number[]): Promise<void> {
        logger.info(`Deleting ${connectionIds.length} connections for workflow ${workflowId}`);
        
        try {
            await Promise.all(
                connectionIds.map(connectionId => this.deleteConnection(projectId, workflowId, connectionId))
            );
            
            logger.info(`Successfully deleted ${connectionIds.length} connections for workflow ${workflowId}`);
        } catch (error) {
            logger.error(`Failed to delete batch connections for workflow ${workflowId}`, error);
            throw transformApiError(error, 'Failed to delete batch connections');
        }
    }

    // Helper methods for common connection patterns
    async connectStagesSequentially(projectId: number, workflowId: number, stageIds: number[]): Promise<WorkflowStageConnection[]> {
        logger.info(`Creating sequential connections for workflow ${workflowId}`, stageIds);
        
        if (stageIds.length < 2) {
            logger.warn('Need at least 2 stages to create sequential connections');
            return [];
        }

        const connections: CreateWorkflowStageConnectionRequest[] = [];
        for (let i = 0; i < stageIds.length - 1; i++) {
            connections.push({
                fromStageId: stageIds[i],
                toStageId: stageIds[i + 1]
            });
        }

        return this.createConnectionsBatch(projectId, workflowId, connections);
    }

    async validateConnection(projectId: number, workflowId: number, fromStageId: number, toStageId: number): Promise<boolean> {
        try {
            // Check if both stages exist and belong to the workflow
            const { workflowStageService } = await import('./workflowStageService');
            
            const [fromStage, toStage] = await Promise.all([
                workflowStageService.getWorkflowStage(projectId, workflowId, fromStageId),
                workflowStageService.getWorkflowStage(projectId, workflowId, toStageId)
            ]);

            // Basic validation - stages exist and belong to the workflow
            const isValid = fromStage.workflowId === workflowId && toStage.workflowId === workflowId;
            
            if (!isValid) {
                logger.warn(`Invalid connection: stages ${fromStageId} or ${toStageId} do not belong to workflow ${workflowId}`);
            }

            return isValid;
        } catch (error) {
            logger.error(`Failed to validate connection from ${fromStageId} to ${toStageId}`, error);
            return false;
        }
    }
}

export const workflowStageConnectionService = new WorkflowStageConnectionService();
