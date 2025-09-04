import type { Task, TaskStatus } from '@/services/project/task/task.types';
import type { TaskResult, TaskService, PermissionsService } from './taskManager.types';
import type { TimerService } from '@/core/timeTracking';
import { AppLogger } from '@/core/logger/logger';

/**
 * Core business logic for task management operations.
 * Handles task completion, suspension, deferring, veto operations,
 * and task status validations.
 */
export class TaskManager {
    private logger = AppLogger.createServiceLogger('TaskManager');

    constructor(
        private taskService: TaskService,
        private permissions: PermissionsService,
        private timer: TimerService
    ) {}

    /**
     * Complete a task using the pipeline system
     * @param projectId - The project ID
     * @param taskId - The task ID to complete
     * @returns Promise resolving to TaskResult
     */
    async completeTask(projectId: number, taskId: number): Promise<TaskResult> {
        try {
            this.logger.info('Completing task using pipeline system', { taskId });
            
            const result = await this.taskService.completeTaskPipeline(projectId, taskId);
            
            if (!result.isSuccess) {
                return {
                    success: false,
                    error: result.errorMessage || 'Task completion failed'
                };
            }
            
            // Refresh current task data
            const updatedTask = await this.taskService.getTaskById(projectId, taskId);
            
            this.logger.info(`Successfully completed task ${taskId} via pipeline`, { details: result.details });
            
            return {
                success: true,
                task: updatedTask,
                message: `Task ${taskId} successfully completed via pipeline`,
                error: undefined
            };
        } catch (error) {
            this.logger.error('Failed to complete task via pipeline:', error);
            return {
                success: false,
                error: error instanceof Error ? error.message : 'Failed to complete task'
            };
        }
    }

    /**
     * Suspend a task with working time preservation
     * @param projectId - The project ID
     * @param taskId - The task ID to suspend
     * @param currentWorkingTime - Current working time in milliseconds
     * @returns Promise resolving to TaskResult
     */
    async suspendTask(projectId: number, taskId: number, currentWorkingTime: number): Promise<TaskResult> {
        try {
            // Calculate total working time including current session
            const totalWorkingTime = this.calculateTotalWorkingTime(currentWorkingTime);
            
            this.logger.info('Suspending task with working time preservation', { 
                taskId, 
                currentTime: currentWorkingTime, 
                totalTime: totalWorkingTime 
            });
            
            // Use the shared working time preservation logic
            const suspendedTask = await this._saveWorkingTimeAndChangeStatus(
                () => this.taskService.changeTaskStatus(projectId, taskId, { targetStatus: 'SUSPENDED' as TaskStatus }),
                'suspension',
                projectId,
                taskId,
                totalWorkingTime
            );
            
            this.logger.info(`Successfully suspended task ${taskId}`);
            
            return {
                success: true,
                task: suspendedTask,
                message: `Task ${taskId} successfully suspended`,
                error: undefined
            };
        } catch (error) {
            this.logger.error('Failed to suspend task:', error);
            return {
                success: false,
                error: error instanceof Error ? error.message : 'Failed to suspend task'
            };
        }
    }

    /**
     * Defer a task (skip for now) with working time preservation
     * @param projectId - The project ID  
     * @param taskId - The task ID to defer
     * @param currentWorkingTime - Current working time in milliseconds
     * @returns Promise resolving to TaskResult
     */
    async deferTask(projectId: number, taskId: number, currentWorkingTime: number): Promise<TaskResult> {
        try {
            // Calculate total working time including current session
            const totalWorkingTime = this.calculateTotalWorkingTime(currentWorkingTime);
            
            this.logger.info('Deferring task with working time preservation', { 
                taskId, 
                currentTime: currentWorkingTime, 
                totalTime: totalWorkingTime 
            });
            
            // Use the shared working time preservation logic
            const deferredTask = await this._saveWorkingTimeAndChangeStatus(
                () => this.taskService.changeTaskStatus(projectId, taskId, { targetStatus: 'DEFERRED' as TaskStatus }),
                'deferring',
                projectId,
                taskId,
                totalWorkingTime
            );
            
            this.logger.info(`Successfully deferred task ${taskId}`);
            
            return {
                success: true,
                task: deferredTask,
                message: `Task ${taskId} successfully deferred`,
                error: undefined
            };
        } catch (error) {
            this.logger.error('Failed to defer task:', error);
            return {
                success: false,
                error: error instanceof Error ? error.message : 'Failed to defer task'
            };
        }
    }

    /**
     * Veto/return a task for rework using the veto pipeline
     * @param projectId - The project ID
     * @param taskId - The task ID to veto
     * @param reason - Optional reason for the veto
     * @returns Promise resolving to TaskResult
     */
    async vetoTask(projectId: number, taskId: number, reason?: string): Promise<TaskResult> {
        try {
            const vetoReason = reason || 'Task returned for rework';
            
            this.logger.info('Returning task for rework using veto pipeline', { taskId, reason: vetoReason });
            
            // Use the veto pipeline to handle all operations atomically
            const pipelineResult = await this.taskService.vetoTaskPipeline(projectId, taskId, {
                reason: vetoReason
            });
            
            if (!pipelineResult.isSuccess) {
                return {
                    success: false,
                    error: pipelineResult.errorMessage || 'Veto pipeline failed'
                };
            }
            
            this.logger.info(`Successfully returned task ${taskId} for rework`, { reason: vetoReason });
            
            return {
                success: true,
                task: pipelineResult.updatedTask,
                message: `Task ${taskId} successfully returned for rework`,
                error: undefined
            };
        } catch (error) {
            this.logger.error('Failed to return task for rework:', error);
            return {
                success: false,
                error: error instanceof Error ? error.message : 'Failed to return task for rework'
            };
        }
    }

    /**
     * Check if a task can be completed based on its current status
     * @param task - The task to check
     * @returns Boolean indicating if the task can be completed
     */
    canCompleteTask(task: Task): boolean {
        // Only tasks that are in progress or ready states can be completed
        const completableStatuses = [
            'IN_PROGRESS' as TaskStatus,
            'READY_FOR_ANNOTATION' as TaskStatus,
            'READY_FOR_REVIEW' as TaskStatus,
            'READY_FOR_COMPLETION' as TaskStatus,
            'CHANGES_REQUIRED' as TaskStatus
        ];
        
        return task.status ? completableStatuses.includes(task.status) : false;
    }

    /**
     * Check if a task can be opened by the current user
     * @param task - The task to check
     * @returns Promise resolving to boolean indicating if the task can be opened
     */
    async canOpenTask(task: Task): Promise<boolean> {
        // Deferred tasks can only be opened by managers
        if (task.status === 'DEFERRED') {
            return await this.permissions.canUpdateProject();
        }
        
        // Vetoed tasks cannot be opened (they are view-only)
        if (task.status === 'VETOED') {
            return false;
        }
        
        // Completed and archived tasks cannot be opened
        if (task.status && ['COMPLETED', 'ARCHIVED'].includes(task.status as string)) {
            return false;
        }
        
        // All other tasks can be opened if user has basic permissions
        return true;
    }

    /**
     * Calculate total working time including current timer session
     * @param currentWorkingTime - Current saved working time in milliseconds
     * @returns Total working time in milliseconds
     */
    calculateTotalWorkingTime(currentWorkingTime: number): number {
        if (!this.timer.isRunning()) {
            return currentWorkingTime;
        }
        
        const elapsedTime = this.timer.getElapsedTime();
        return currentWorkingTime + elapsedTime;
    }

    /**
     * Common helper to save working time and preserve it across task status changes
     * @private
     * @param statusChangeOperation - Function that performs the status change and returns the updated task
     * @param operationName - Name of the operation for logging (e.g., "completion", "suspension")
     * @param projectId - The project ID
     * @param taskId - The task ID  
     * @param finalWorkingTime - The final working time to preserve
     */
    private async _saveWorkingTimeAndChangeStatus<T extends Task>(
        statusChangeOperation: () => Promise<T>,
        operationName: string,
        projectId: number,
        taskId: number,
        finalWorkingTime: number
    ): Promise<T> {
        this.logger.info(`Starting ${operationName} with working time preservation`, { 
            taskId, 
            finalWorkingTime 
        });
        
        // Execute the status change operation
        const updatedTask = await statusChangeOperation();
        
        // Ensure the working time is preserved in case the backend didn't return the latest value
        if (updatedTask.workingTimeMs < finalWorkingTime) {
            this.logger.warn(`Working time mismatch after ${operationName}. Backend: ${updatedTask.workingTimeMs}ms, Expected: ${finalWorkingTime}ms. Updating...`);
            try {
                const correctedTask = await this.taskService.updateWorkingTime(projectId, taskId, finalWorkingTime);
                this.logger.info(`Working time corrected after ${operationName}`, { 
                    taskId, 
                    correctedTime: correctedTask.workingTimeMs 
                });
                return correctedTask as T;
            } catch (correctionError) {
                this.logger.error(`Failed to correct working time after ${operationName}:`, correctionError);
                // Return the original task even if correction failed
                return updatedTask;
            }
        }
        
        this.logger.info(`${operationName} completed successfully with preserved working time`, { 
            taskId, 
            workingTime: updatedTask.workingTimeMs 
        });
        return updatedTask;
    }
}