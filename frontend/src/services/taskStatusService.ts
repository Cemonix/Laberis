import { taskService, workflowStageService } from '@/services/api/projects';
import { TaskStatus, type Task, type ChangeTaskStatusDto } from '@/types/task';
import { WorkflowStageType, type WorkflowStage } from '@/types/workflow/workflowstage';
import { AppLogger } from '@/utils/logger';

const logger = AppLogger.createServiceLogger('TaskStatusService');

/**
 * Context-aware task status service that implements the transition matrix logic
 * from the task status refactoring plan.
 */
export class TaskStatusService {
    private stageCache = new Map<number, WorkflowStage>();

    /**
     * Get workflow stage with caching
     */
    private async getWorkflowStage(projectId: number, workflowId: number, stageId: number): Promise<WorkflowStage> {
        if (this.stageCache.has(stageId)) {
            return this.stageCache.get(stageId)!;
        }

        const stage = await workflowStageService.getWorkflowStageById(projectId, workflowId, stageId);
        this.stageCache.set(stageId, stage);
        return stage;
    }

    /**
     * Determine if asset movement should occur for a status transition
     */
    private shouldMoveAsset(
        _fromStatus: TaskStatus, 
        toStatus: TaskStatus, 
        stageType: WorkflowStageType,
        isFinalStage: boolean
    ): boolean {
        // Only COMPLETED status may trigger asset movement
        if (toStatus !== TaskStatus.COMPLETED) {
            return false;
        }

        // Final completion stage doesn't move assets
        if (stageType === WorkflowStageType.COMPLETION && isFinalStage) {
            return false;
        }

        // Annotation and Review stages move assets when completed
        if (stageType === WorkflowStageType.ANNOTATION || stageType === WorkflowStageType.REVISION) {
            return true;
        }

        // Completion stage (non-final) doesn't move assets
        return false;
    }

    /**
     * Validate if a status transition is allowed based on the transition matrix
     */
    private isTransitionAllowed(
        fromStatus: TaskStatus,
        toStatus: TaskStatus,
        stageType: WorkflowStageType,
        isManager: boolean = false
    ): { allowed: boolean; reason?: string } {
        // Manager can override most transitions (except some final states)
        if (isManager && fromStatus === TaskStatus.DEFERRED) {
            return { allowed: true };
        }

        // Manager-only transitions in completion stages
        if (stageType === WorkflowStageType.COMPLETION) {
            if (!isManager) {
                return { allowed: false, reason: 'Completion stage tasks require manager permissions. Only project managers can work in completion stages.' };
            }
        }

        // Check specific transition rules
        switch (fromStatus) {
            case TaskStatus.READY_FOR_ANNOTATION:
                const allowedFromReadyAnnotation = [TaskStatus.IN_PROGRESS];
                if (isManager) {
                    allowedFromReadyAnnotation.push(TaskStatus.DEFERRED, TaskStatus.SUSPENDED);
                }
                return allowedFromReadyAnnotation.includes(toStatus)
                    ? { allowed: true }
                    : { allowed: false, reason: 'Can only start work from ready state' };

            case TaskStatus.READY_FOR_REVIEW:
                const allowedFromReadyReview = [TaskStatus.IN_PROGRESS];
                if (isManager) {
                    allowedFromReadyReview.push(TaskStatus.DEFERRED, TaskStatus.SUSPENDED);
                }
                return allowedFromReadyReview.includes(toStatus)
                    ? { allowed: true }
                    : { allowed: false, reason: 'Can only start work from ready state' };

            case TaskStatus.READY_FOR_COMPLETION:
                if (!isManager) {
                    return { allowed: false, reason: 'Completion stage requires manager permissions' };
                }
                const allowedFromReadyCompletion = [TaskStatus.IN_PROGRESS, TaskStatus.DEFERRED, TaskStatus.SUSPENDED];
                return allowedFromReadyCompletion.includes(toStatus)
                    ? { allowed: true }
                    : { allowed: false, reason: 'Can only start work from ready state' };

            case TaskStatus.IN_PROGRESS:
                const allowedFromProgress = [
                    TaskStatus.COMPLETED,
                    TaskStatus.SUSPENDED,
                    TaskStatus.DEFERRED
                ];
                return allowedFromProgress.includes(toStatus)
                    ? { allowed: true }
                    : { allowed: false, reason: `Cannot transition from IN_PROGRESS to ${toStatus}` };

            case TaskStatus.SUSPENDED:
                const allowedFromSuspended = [TaskStatus.IN_PROGRESS, TaskStatus.DEFERRED];
                return allowedFromSuspended.includes(toStatus)
                    ? { allowed: true }
                    : { allowed: false, reason: 'Can only resume suspended tasks to IN_PROGRESS or defer them' };

            case TaskStatus.COMPLETED:
                if (stageType === WorkflowStageType.COMPLETION) {
                    const allowedFromCompleted = [
                        TaskStatus.ARCHIVED,
                        TaskStatus.READY_FOR_ANNOTATION // Manager can send back to annotation
                    ];
                    if (!isManager && toStatus !== TaskStatus.ARCHIVED) {
                        return { allowed: false, reason: 'Only managers can modify completed tasks in completion stage' };
                    }
                    return allowedFromCompleted.includes(toStatus)
                        ? { allowed: true }
                        : { allowed: false, reason: `Cannot transition from COMPLETED to ${toStatus} in completion stage` };
                } else {
                    // In annotation/review stages, completed tasks get archived automatically
                    return toStatus === TaskStatus.IN_PROGRESS
                        ? { allowed: true }
                        : { allowed: false, reason: 'Can only uncomplete to IN_PROGRESS' };
                }

            case TaskStatus.ARCHIVED:
                return { allowed: false, reason: 'Cannot modify archived tasks' };

            case TaskStatus.DEFERRED:
                return isManager
                    ? { allowed: true }
                    : { allowed: false, reason: 'Only managers can modify deferred tasks' };

            case TaskStatus.CHANGES_REQUIRED:
                // Tasks marked for changes can be suspended, deferred, or put back in progress
                const allowedFromChangesRequired = [TaskStatus.IN_PROGRESS, TaskStatus.SUSPENDED, TaskStatus.DEFERRED];
                return allowedFromChangesRequired.includes(toStatus)
                    ? { allowed: true }
                    : { allowed: false, reason: `Cannot transition from CHANGES_REQUIRED to ${toStatus}` };

            case TaskStatus.VETOED:
                // Vetoed tasks can be suspended, deferred, or put back in progress
                const allowedFromVetoed = [TaskStatus.IN_PROGRESS, TaskStatus.SUSPENDED, TaskStatus.DEFERRED];
                return allowedFromVetoed.includes(toStatus)
                    ? { allowed: true }
                    : { allowed: false, reason: `Cannot transition from VETOED to ${toStatus}` };

            default:
                return { allowed: false, reason: `Unknown status transition from ${fromStatus} to ${toStatus}` };
        }
    }

    /**
     * Change task status with full context awareness and validation
     */
    async changeTaskStatus(
        projectId: number,
        task: Task,
        targetStatus: TaskStatus,
        isManager: boolean = false
    ): Promise<Task> {
        logger.info(`Changing task ${task.id} status from ${task.status} to ${targetStatus}`, {
            projectId,
            taskId: task.id,
            currentStatus: task.status,
            targetStatus,
            isManager
        });

        // Get workflow stage information
        const stage = await this.getWorkflowStage(projectId, task.workflowId, task.currentWorkflowStageId);

        // Validate transition
        const validation = this.isTransitionAllowed(
            task.status || TaskStatus.NOT_STARTED,
            targetStatus,
            stage.stageType || WorkflowStageType.ANNOTATION,
            isManager
        );

        if (!validation.allowed) {
            throw new Error(`Invalid transition: ${validation.reason}`);
        }

        // Determine asset movement
        const moveAsset = this.shouldMoveAsset(
            task.status || TaskStatus.NOT_STARTED,
            targetStatus,
            stage.stageType || WorkflowStageType.ANNOTATION,
            stage.isFinalStage
        );

        // Create request DTO
        const requestDto: ChangeTaskStatusDto = {
            targetStatus,
            moveAsset
        };

        logger.info(`Status change validated`, {
            taskId: task.id,
            transition: `${task.status} -> ${targetStatus}`,
            stageType: stage.stageType,
            moveAsset,
            isFinalStage: stage.isFinalStage
        });

        // Execute the status change
        return await taskService.changeTaskStatus(projectId, task.id, requestDto);
    }

    /**
     * Complete a task with proper context awareness
     */
    async completeTask(projectId: number, task: Task, isManager: boolean = false): Promise<Task> {
        logger.info(`Completing task ${task.id} with context awareness`, {
            projectId,
            taskId: task.id,
            currentStatus: task.status,
            isManager
        });

        // Get workflow stage information
        const stage = await this.getWorkflowStage(projectId, task.workflowId, task.currentWorkflowStageId);
        
        // Determine if asset movement should occur
        const shouldMoveAsset = this.shouldMoveAsset(
            task.status || TaskStatus.NOT_STARTED,
            TaskStatus.COMPLETED,
            stage.stageType || WorkflowStageType.ANNOTATION,
            stage.isFinalStage
        );
        
        if (shouldMoveAsset) {
            logger.info(`Task ${task.id} completion will trigger asset movement`, {
                stageType: stage.stageType,
                isFinalStage: stage.isFinalStage
            });
            // Use the dedicated complete-and-move endpoint for automatic asset movement
            return await taskService.completeAndMoveTask(projectId, task.id);
        } else {
            logger.info(`Task ${task.id} completion will not trigger asset movement`, {
                stageType: stage.stageType,
                isFinalStage: stage.isFinalStage
            });
            // Use the regular status change endpoint
            return this.changeTaskStatus(projectId, task, TaskStatus.COMPLETED, isManager);
        }
    }

    /**
     * Suspend a task
     */
    async suspendTask(projectId: number, task: Task, isManager: boolean = false): Promise<Task> {
        return this.changeTaskStatus(projectId, task, TaskStatus.SUSPENDED, isManager);
    }

    /**
     * Resume (unsuspend) a task
     */
    async resumeTask(projectId: number, task: Task, isManager: boolean = false): Promise<Task> {
        // Resuming goes to IN_PROGRESS, backend will determine proper ready status
        return this.changeTaskStatus(projectId, task, TaskStatus.IN_PROGRESS, isManager);
    }

    /**
     * Defer a task
     */
    async deferTask(projectId: number, task: Task, isManager: boolean = false): Promise<Task> {
        return this.changeTaskStatus(projectId, task, TaskStatus.DEFERRED, isManager);
    }

    /**
     * Undefer a task (return to normal workflow state)
     */
    async undeferTask(projectId: number, task: Task, isManager: boolean = false): Promise<Task> {
        // Undeferring goes to IN_PROGRESS, backend will determine proper ready status based on assignment
        return this.changeTaskStatus(projectId, task, TaskStatus.IN_PROGRESS, isManager);
    }

    /**
     * Uncomplete a task (mark as in progress again)
     */
    async uncompleteTask(projectId: number, task: Task, isManager: boolean = false): Promise<Task> {
        return this.changeTaskStatus(projectId, task, TaskStatus.IN_PROGRESS, isManager);
    }

    /**
     * Archive a task (completion stage only)
     */
    async archiveTask(projectId: number, task: Task, isManager: boolean = false): Promise<Task> {
        return this.changeTaskStatus(projectId, task, TaskStatus.ARCHIVED, isManager);
    }

    /**
     * Send a completed task back to annotation (completion stage manager only)
     */
    async sendBackToAnnotation(projectId: number, task: Task, isManager: boolean = true): Promise<Task> {
        return this.changeTaskStatus(projectId, task, TaskStatus.READY_FOR_ANNOTATION, isManager);
    }

    /**
     * Return a task for rework using the dedicated endpoint
     * Available to reviewers (from review stages) and managers (from completion stages)
     */
    async returnTaskForRework(projectId: number, task: Task, reason?: string): Promise<Task> {
        logger.info(`Returning task ${task.id} for rework`, {
            projectId,
            taskId: task.id,
            currentStatus: task.status,
            reason
        });

        // Use the specialized return for rework endpoint
        return await taskService.returnTaskForRework(projectId, task.id, reason);
    }

    /**
     * Clear stage cache (useful when stages are modified)
     */
    clearCache(): void {
        this.stageCache.clear();
    }
}

// Export singleton instance
export const taskStatusService = new TaskStatusService();