import { AppLogger } from '@/utils/logger';
import { ProjectRole } from '@/types/project/project';
import { WorkflowStageType } from '@/types/workflow/workflowstage';
import type { WorkflowStage, Workflow } from '@/types/workflow';
import { faEdit, faCheckCircle, faTasks, faEye, faDiagramProject } from '@fortawesome/free-solid-svg-icons';
import type { IconDefinition } from '@fortawesome/fontawesome-svg-core';

const logger = AppLogger.createServiceLogger('WorkflowNavigationUtil');

/**
 * Role-based workflow navigation utility
 * Maps user roles to appropriate workflow stages for direct task access
 */
export class WorkflowNavigationHelper {
    
    /**
     * Map user roles to their primary workflow stage types
     */
    private static readonly ROLE_STAGE_MAPPING: Record<ProjectRole, WorkflowStageType> = {
        [ProjectRole.ANNOTATOR]: WorkflowStageType.ANNOTATION,
        [ProjectRole.REVIEWER]: WorkflowStageType.REVISION,
        [ProjectRole.MANAGER]: WorkflowStageType.COMPLETION,
        [ProjectRole.VIEWER]: WorkflowStageType.ANNOTATION, // Viewers default to annotation stage for viewing
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
        const targetStageType = this.ROLE_STAGE_MAPPING[userRole];
        
        // Find stage matching the user's role
        const roleStage = stages.find(stage => stage.stageType === targetStageType);
        if (roleStage) {
            logger.info(`Found ${targetStageType} stage for ${userRole}: ${roleStage.name}`);
            return roleStage;
        }

        // Fallback strategies
        logger.warn(`No ${targetStageType} stage found for ${userRole}, using fallback`);
        
        // For managers, try annotation stage as fallback
        if (userRole === ProjectRole.MANAGER) {
            const annotationStage = stages.find(stage => stage.stageType === WorkflowStageType.ANNOTATION);
            if (annotationStage) {
                logger.info(`Manager fallback: using annotation stage ${annotationStage.name}`);
                return annotationStage;
            }
        }

        // For viewers and others, use the first stage
        if (stages.length > 0) {
            const firstStage = stages[0];
            logger.info(`Using first stage as fallback: ${firstStage.name}`);
            return firstStage;
        }

        logger.warn('No suitable stage found for user role');
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

    // TODO: Implement workflow edit functionality
    // static editWorkflow(workflowId: number): void {
    //     // Implementation needed for editing workflow details, stages, and configurations
    // }

    // TODO: Implement workflow deletion functionality  
    // static deleteWorkflow(workflowId: number): Promise<void> {
    //     // Implementation needed for safe workflow deletion with validation checks
    // }

    // TODO: Implement workflow update functionality
    // static updateWorkflow(workflowId: number, updates: Partial<Workflow>): Promise<void> {
    //     // Implementation needed for updating workflow properties and stage configurations
    // }
}