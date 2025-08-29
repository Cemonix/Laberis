import { BaseProjectService } from '../baseProjectService';
import type {WorkflowStageAssignment, CreateWorkflowStageAssignmentRequest} from './workflowStage.types';

/**
 * Service class for managing workflow stage assignments within projects.
 */
class WorkflowStageAssignmentService extends BaseProjectService {
    constructor() {
        super('WorkflowStageAssignmentService');
    }

    /**
     * Gets all assignments for a specific workflow stage
     */
    async getStageAssignments(
        projectId: number,
        workflowId: number,
        stageId: number
    ): Promise<WorkflowStageAssignment[]> {
        this.logger.info(`Fetching assignments for stage ${stageId} in workflow ${workflowId}, project ${projectId}`);
        
        const url = this.buildProjectResourceUrl(
            projectId, 
            'workflows/{workflowId}/stages/{stageId}/assignments', 
            { workflowId, stageId }
        );
        const response = await this.get<WorkflowStageAssignment[]>(url);
        
        this.logger.info(`Successfully fetched ${response.length} assignments for stage ${stageId}`);
        return response;
    }

    /**
     * Assigns a project member to a workflow stage
     */
    async createStageAssignment(
        projectId: number,
        workflowId: number,
        stageId: number,
        projectMemberId: number
    ): Promise<WorkflowStageAssignment> {
        this.logger.info(`Creating assignment for member ${projectMemberId} to stage ${stageId} in workflow ${workflowId}, project ${projectId}`);
        
        const data: CreateWorkflowStageAssignmentRequest = {
            workflowStageId: stageId,
            projectMemberId: projectMemberId
        };
        
        const url = this.buildProjectResourceUrl(
            projectId, 
            'workflows/{workflowId}/stages/{stageId}/assignments', 
            { workflowId, stageId }
        );
        const response = await this.post<CreateWorkflowStageAssignmentRequest, WorkflowStageAssignment>(url, data);
        
        this.logger.info(`Successfully created assignment: ${response.id} for member ${projectMemberId} to stage ${stageId}`);
        return response;
    }

    /**
     * Removes a project member assignment from a workflow stage
     */
    async deleteStageAssignment(
        projectId: number,
        workflowId: number,
        stageId: number,
        assignmentId: number
    ): Promise<void> {
        this.logger.info(`Deleting assignment ${assignmentId} from stage ${stageId} in workflow ${workflowId}, project ${projectId}`);
        
        const url = this.buildProjectResourceUrl(
            projectId, 
            'workflows/{workflowId}/stages/{stageId}/assignments/{assignmentId}', 
            { workflowId, stageId, assignmentId }
        );
        await this.delete(url);
        
        this.logger.info(`Successfully deleted assignment ${assignmentId} from stage ${stageId}`);
    }

    /**
     * Gets assignments by project member ID across all workflow stages in a project
     */
    async getAssignmentsForProjectMember(
        projectId: number,
        projectMemberId: number
    ): Promise<WorkflowStageAssignment[]> {
        this.logger.info(`Fetching all assignments for project member ${projectMemberId} in project ${projectId}`);
        
        // TODO: Implement this method when backend endpoint is available.

        this.logger.warn('getAssignmentsForProjectMember is not implemented - backend endpoint needed');
        return [];
    }
}

export const workflowStageAssignmentService = new WorkflowStageAssignmentService();