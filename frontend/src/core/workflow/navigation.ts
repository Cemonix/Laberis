import { AppLogger } from '@/core/logger/logger';
import { ProjectRole } from '@/services/project/project.types';
import { WorkflowStageType } from '@/services/project/workflow/workflowStage.types';
import type { WorkflowStage } from '@/services/project/workflow/workflowStage.types';
import type { Workflow } from '@/services/project/workflow/workflow.types';
import { faEdit, faCheckCircle, faTasks, faEye, faDiagramProject } from '@fortawesome/free-solid-svg-icons';
import type { IconDefinition } from '@fortawesome/fontawesome-svg-core';

const logger = AppLogger.createServiceLogger('WorkflowNavigation');

/**
 * Role-based workflow navigation utility
 * Maps user roles to appropriate workflow stages for direct task access
 */
export class WorkflowNavigationHelper {
    
    /**
     * Map user roles to their preferred workflow stage types (in order of preference)
     * Note: ANNOTATION and COMPLETION stages are always present, REVISION is optional
     */
    private static readonly ROLE_STAGE_PREFERENCES: Record<ProjectRole, WorkflowStageType[]> = {
        [ProjectRole.ANNOTATOR]: [WorkflowStageType.ANNOTATION],
        [ProjectRole.REVIEWER]: [WorkflowStageType.REVISION, WorkflowStageType.COMPLETION], // If no review stage, fall back to completion for oversight
        [ProjectRole.MANAGER]: [WorkflowStageType.COMPLETION], // Managers primarily work in completion stage
        [ProjectRole.VIEWER]: [WorkflowStageType.ANNOTATION, WorkflowStageType.REVISION, WorkflowStageType.COMPLETION], // Viewers can view any stage
    };

    /**
     * Get button text based on user role
     */
    static getButtonText(userRole: ProjectRole): string {
        switch (userRole) {
            case ProjectRole.ANNOTATOR:
                return 'Annotate Data';
            case ProjectRole.REVIEWER:
                return 'Review Data';
            case ProjectRole.MANAGER:
                return 'Manage Tasks';
            case ProjectRole.VIEWER:
                return 'View Tasks';
            default:
                return 'View Pipeline';
        }
    }

    /**
     * Get button icon based on user role
     */
    static getButtonIcon(userRole: ProjectRole): IconDefinition {
        switch (userRole) {
            case ProjectRole.ANNOTATOR:
                return faEdit;
            case ProjectRole.REVIEWER:
                return faCheckCircle;
            case ProjectRole.MANAGER:
                return faTasks;
            case ProjectRole.VIEWER:
                return faEye;
            default:
                return faDiagramProject;
        }
    }

    /**
     * Get appropriate stage for user role from workflow stages
     */
    static getStageForRole(stages: WorkflowStage[], userRole: ProjectRole): WorkflowStage | null {
        const preferences = this.ROLE_STAGE_PREFERENCES[userRole];
        
        // Try each preferred stage type in order
        for (const preferredStageType of preferences) {
            const matchingStage = stages.find(stage => stage.stageType === preferredStageType);
            if (matchingStage) {
                logger.info(`Found ${preferredStageType} stage for ${userRole}: ${matchingStage.name}`);
                return matchingStage;
            }
        }

        // If no preferred stages found, use the first available stage
        if (stages.length > 0) {
            const firstStage = stages[0];
            logger.info(`No preferred stages found for ${userRole}, using first available stage: ${firstStage.name}`);
            return firstStage;
        }

        logger.warn('No stages available in workflow - this may indicate a workflow configuration issue');
        return null;
    }

    /**
     * Generate navigation URL for user role within workflow
     */
    static getNavigationUrl(
        projectId: number,
        workflow: Workflow,
        stages: WorkflowStage[],
        userRole: ProjectRole
    ): { url: string; type: 'tasks' | 'pipeline' } {
        const stage = this.getStageForRole(stages, userRole);
        
        if (stage) {
            // Direct link to stage tasks
            const tasksUrl = `/projects/${projectId}/workflows/${workflow.id}/stages/${stage.id}/tasks`;
            logger.info(`Generated tasks URL for ${userRole}: ${tasksUrl}`);
            return { url: tasksUrl, type: 'tasks' };
        } else {
            // Fallback to pipeline view
            const pipelineUrl = `/projects/${projectId}/workflows/${workflow.id}/pipeline`;
            logger.info(`Generated pipeline URL as fallback: ${pipelineUrl}`);
            return { url: pipelineUrl, type: 'pipeline' };
        }
    }

    /**
     * Check if user has permission to access workflow stage based on assignments
     */
    static canAccessStage(stage: WorkflowStage, userEmail: string): boolean {
        if (!stage.assignments || stage.assignments.length === 0) {
            // If no assignments, assume open access
            return true;
        }

        // Check if user is assigned to this stage
        const isAssigned = stage.assignments.some(
            assignment => assignment.projectMember.email === userEmail
        );

        logger.info(`User ${userEmail} ${isAssigned ? 'can' : 'cannot'} access stage ${stage.name}`);
        return isAssigned;
    }

    /**
     * Get user's accessible stages within a workflow
     */
    static getAccessibleStages(stages: WorkflowStage[], userEmail: string): WorkflowStage[] {
        return stages.filter(stage => this.canAccessStage(stage, userEmail));
    }

    /**
     * Generate navigation info for workflow card
     */
    static getWorkflowCardNavigation(
        projectId: number,
        workflow: Workflow,
        stages: WorkflowStage[],
        userRole: ProjectRole,
        userEmail?: string
    ) {
        // Filter stages user can access if email provided
        const accessibleStages = userEmail 
            ? this.getAccessibleStages(stages, userEmail)
            : stages;

        const navigation = this.getNavigationUrl(projectId, workflow, accessibleStages, userRole);
        const buttonText = this.getButtonText(userRole);
        const buttonIcon = this.getButtonIcon(userRole);

        return {
            url: navigation.url,
            type: navigation.type,
            buttonText,
            buttonIcon,
            hasDirectAccess: navigation.type === 'tasks',
            accessibleStagesCount: accessibleStages.length
        };
    }
}