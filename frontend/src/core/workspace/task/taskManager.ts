import { AppLogger } from '@/core/logger/logger';
import { taskService } from '@/services/project';
import type { Task } from '@/services/project/task/task.types';
import { TaskStatus } from '@/services/project/task/task.types';
import type { TaskNavigationContext } from '@/stores/workspaceStore.types';

const logger = AppLogger.createServiceLogger('TaskManager');

/**
 * Parameters for task operations
 */
export interface TaskOperationParams {
    projectId: number;
    taskId: number;
    workflowStageId?: number;
}

/**
 * Result of task operations
 */
export type TaskOperationResult<T = Task> = {
    success: true;
    data: T;
} | {
    success: false;
    error: string;
};

/**
 * Navigation result with task information
 */
export interface TaskNavigationResult {
    taskId: number | null;
    assetId: number | null;
    hasNext: boolean;
    hasPrevious: boolean;
    currentPosition: number;
    totalTasks: number;
    message?: string | null;
}

/**
 * Task completion pipeline result
 */
export interface TaskPipelineResult {
    isSuccess: boolean;
    updatedTask?: Task;
    errorMessage?: string;
}

/**
 * TaskManager handles all task-related operations outside of Pinia store reactivity.
 * This includes task loading, status changes, navigation, and pipeline operations.
 */
export class TaskManager {
    /**
     * Load a specific task by ID
     */
    async loadTask(projectId: number, taskId: number): Promise<TaskOperationResult<Task>> {
        try {
            logger.info(`Loading task ${taskId} for project ${projectId}`);
            const task = await taskService.getTaskById(projectId, taskId);
            return { success: true, data: task };
        } catch (error) {
            const errorMessage = error instanceof Error ? error.message : 'Failed to load task';
            logger.error(`Failed to load task ${taskId}:`, error);
            return { success: false, error: errorMessage };
        }
    }

    /**
     * Load tasks for a specific asset
     */
    async loadTasksForAsset(projectId: number, assetId: number): Promise<TaskOperationResult<Task[]>> {
        try {
            logger.info(`Loading tasks for asset ${assetId} in project ${projectId}`);
            const tasks = await taskService.getTasksForAsset(projectId, assetId);
            return { success: true, data: tasks };
        } catch (error) {
            const errorMessage = error instanceof Error ? error.message : 'Failed to load tasks for asset';
            logger.error(`Failed to load tasks for asset ${assetId}:`, error);
            return { success: false, error: errorMessage };
        }
    }

    /**
     * Load assigned tasks for a specific workflow stage
     */
    async loadMyTasksForStage(projectId: number, workflowStageId: number): Promise<TaskOperationResult<Task[]>> {
        try {
            logger.info(`Loading my tasks for stage ${workflowStageId} in project ${projectId}`);
            const assignedTasks = await taskService.getMyTasksForStage(projectId, workflowStageId);
            return { success: true, data: assignedTasks.tasks };
        } catch (error) {
            const errorMessage = error instanceof Error ? error.message : 'Failed to load assigned tasks';
            logger.error(`Failed to load assigned tasks for stage ${workflowStageId}:`, error);
            return { success: false, error: errorMessage };
        }
    }

    /**
     * Get navigation context for a task
     */
    async getNavigationContext(
        projectId: number, 
        taskId: number, 
        workflowStageId: number
    ): Promise<TaskOperationResult<TaskNavigationContext>> {
        try {
            logger.info(`Loading navigation context for task ${taskId} in stage ${workflowStageId}`);
            const navigationContext = await taskService.getNavigationContext(
                projectId, 
                taskId,
                workflowStageId
            );
            
            const context: TaskNavigationContext = {
                hasNext: navigationContext.hasNext,
                hasPrevious: navigationContext.hasPrevious,
                currentPosition: navigationContext.currentPosition,
                totalTasks: navigationContext.totalTasks,
                message: navigationContext.message
            };
            
            return { success: true, data: context };
        } catch (error) {
            const errorMessage = error instanceof Error ? error.message : 'Failed to load navigation context';
            logger.warn('Failed to load navigation context:', error);
            return { success: false, error: errorMessage };
        }
    }

    /**
     * Navigate to the previous task
     */
    async navigateToPreviousTask(
        projectId: number, 
        taskId: number, 
        workflowStageId: number
    ): Promise<TaskOperationResult<TaskNavigationResult>> {
        try {
            logger.info(`Navigating to previous task from ${taskId} in stage ${workflowStageId}`);
            const navigation = await taskService.getPreviousTask(projectId, taskId, workflowStageId);

            const result: TaskNavigationResult = {
                taskId: navigation.taskId || null,
                assetId: navigation.assetId || null,
                hasNext: navigation.hasNext,
                hasPrevious: navigation.hasPrevious,
                currentPosition: navigation.currentPosition,
                totalTasks: navigation.totalTasks,
                message: navigation.message
            };

            return { success: true, data: result };
        } catch (error) {
            const errorMessage = error instanceof Error ? error.message : 'Failed to navigate to previous task';
            logger.error('Failed to navigate to previous task:', error);
            return { success: false, error: errorMessage };
        }
    }

    /**
     * Navigate to the next task
     */
    async navigateToNextTask(
        projectId: number, 
        taskId: number, 
        workflowStageId: number
    ): Promise<TaskOperationResult<TaskNavigationResult>> {
        try {
            logger.info(`Navigating to next task from ${taskId} in stage ${workflowStageId}`);
            const navigation = await taskService.getNextTask(projectId, taskId, workflowStageId);

            const result: TaskNavigationResult = {
                taskId: navigation.taskId || null,
                assetId: navigation.assetId || null,
                hasNext: navigation.hasNext,
                hasPrevious: navigation.hasPrevious,
                currentPosition: navigation.currentPosition,
                totalTasks: navigation.totalTasks,
                message: navigation.message
            };

            return { success: true, data: result };
        } catch (error) {
            const errorMessage = error instanceof Error ? error.message : 'Failed to navigate to next task';
            logger.error('Failed to navigate to next task:', error);
            return { success: false, error: errorMessage };
        }
    }

    /**
     * Change task status using the TaskService (ensures lock is acquired first)
     */
    async changeTaskStatus(
        projectId: number, 
        taskId: number, 
        targetStatus: TaskStatus
    ): Promise<TaskOperationResult<Task>> {
        try {
            logger.info(`Changing task ${taskId} status to ${targetStatus}`);
            
            // First, ensure we have a lock on the task by fetching it with autoAssign=true
            logger.debug(`Acquiring lock for task ${taskId} before status change`);
            await taskService.getTaskById(projectId, taskId, true);
            
            // Now change the status
            const updatedTask = await taskService.changeTaskStatus(projectId, taskId, { targetStatus });
            
            if (!updatedTask) {
                return { success: false, error: 'Failed to change task status' };
            }
            
            return { success: true, data: updatedTask };
        } catch (error) {
            const errorMessage = error instanceof Error ? error.message : 'Failed to change task status';
            logger.error(`Failed to change task ${taskId} status to ${targetStatus}:`, error);
            return { success: false, error: errorMessage };
        }
    }

    /**
     * Complete task using the pipeline system
     */
    async completeTaskPipeline(projectId: number, taskId: number): Promise<TaskOperationResult<TaskPipelineResult>> {
        try {
            logger.info(`Completing task ${taskId} via pipeline`);
            const result = await taskService.completeTaskPipeline(projectId, taskId);
            return { success: true, data: result };
        } catch (error) {
            const errorMessage = error instanceof Error ? error.message : 'Failed to complete task via pipeline';
            logger.error(`Failed to complete task ${taskId} via pipeline:`, error);
            return { success: false, error: errorMessage };
        }
    }

    /**
     * Veto/return task for rework using the pipeline system
     */
    async vetoTaskPipeline(
        projectId: number, 
        taskId: number, 
        reason?: string
    ): Promise<TaskOperationResult<TaskPipelineResult>> {
        try {
            logger.info(`Vetoing task ${taskId} via pipeline`);
            const result = await taskService.vetoTaskPipeline(projectId, taskId, { 
                reason: reason || 'Task returned for rework' 
            });
            return { success: true, data: result };
        } catch (error) {
            const errorMessage = error instanceof Error ? error.message : 'Failed to veto task via pipeline';
            logger.error(`Failed to veto task ${taskId} via pipeline:`, error);
            return { success: false, error: errorMessage };
        }
    }

    /**
     * Suspend task
     */
    async suspendTask(projectId: number, taskId: number): Promise<TaskOperationResult<Task>> {
        return this.changeTaskStatus(projectId, taskId, TaskStatus.SUSPENDED);
    }

    /**
     * Defer task
     */
    async deferTask(projectId: number, taskId: number): Promise<TaskOperationResult<Task>> {
        return this.changeTaskStatus(projectId, taskId, TaskStatus.DEFERRED);
    }

    /**
     * Assign task to current user and start it
     */
    async assignAndStartTask(projectId: number, taskId: number): Promise<TaskOperationResult<void>> {
        try {
            logger.info(`Assigning and starting task ${taskId}`);
            
            // First assign the task to current user
            await taskService.assignTaskToCurrentUser(projectId, taskId);
            
            // Then change status to IN_PROGRESS using our wrapper method (which ensures lock)
            const result = await this.changeTaskStatus(projectId, taskId, TaskStatus.IN_PROGRESS);
            
            if (!result.success) {
                return { success: false, error: result.error };
            }
            
            return { success: true, data: undefined };
        } catch (error) {
            const errorMessage = error instanceof Error ? error.message : 'Failed to assign and start task';
            logger.error(`Failed to assign and start task ${taskId}:`, error);
            return { success: false, error: errorMessage };
        }
    }

    /**
     * Update working time for a task
     */
    async updateWorkingTime(projectId: number, taskId: number, workingTimeMs: number): Promise<TaskOperationResult<Task>> {
        try {
            logger.info(`Updating working time for task ${taskId} to ${workingTimeMs}ms`);
            const updatedTask = await taskService.updateWorkingTime(projectId, taskId, workingTimeMs);
            return { success: true, data: updatedTask };
        } catch (error) {
            const errorMessage = error instanceof Error ? error.message : 'Failed to update working time';
            logger.error(`Failed to update working time for task ${taskId}:`, error);
            return { success: false, error: errorMessage };
        }
    }

    /**
     * Check if a task can be opened by the current user
     */
    canOpenTask(task: Task): boolean {
        // Simple check - most tasks can be opened, except archived
        return task.status !== TaskStatus.ARCHIVED;
    }

    /**
     * Check if a task is completed
     */
    isTaskCompleted(task: Task | null): boolean {
        if (!task) return false;
        
        // Task is NOT considered completed (i.e., should be editable) if it has CHANGES_REQUIRED status
        if (task.status === TaskStatus.CHANGES_REQUIRED) {
            return false;
        }
        
        // Otherwise, check if task is actually completed
        return task.status === TaskStatus.COMPLETED;
    }

    /**
     * Check if time should be tracked for a task
     */
    shouldTrackTime(task: Task | null): boolean {
        // Only track time for active, non-completed tasks
        return !!(task && !task.completedAt && !task.archivedAt);
    }
}

// Export singleton instance
export const taskManager = new TaskManager();