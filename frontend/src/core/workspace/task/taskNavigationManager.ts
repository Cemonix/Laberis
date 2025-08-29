import type { Task } from '@/services/project/task/task.types';
import type { NavigationResult, NavigationInfo } from './taskNavigationManager.types';
import type { PermissionsService } from './taskManager.types';
import { AppLogger } from '@/core/logger/logger';

/**
 * Core business logic for task navigation operations.
 * Handles navigation between tasks, finding next/previous available tasks,
 * and providing navigation state information.
 */
export class TaskNavigationManager {
    private logger = AppLogger.createServiceLogger('TaskNavigationManager');

    constructor(
        private permissions: PermissionsService
    ) {}

    /**
     * Navigate to the next available task that can be opened
     * @param currentTask - Current task
     * @param availableTasks - List of all available tasks
     * @param projectId - Project ID for navigation
     * @returns Promise resolving to NavigationResult
     */
    async navigateToNext(currentTask: Task, availableTasks: Task[], projectId: string): Promise<NavigationResult> {
        if (availableTasks.length === 0) {
            this.logger.warn('Cannot navigate to next task: no available tasks');
            return { success: true, navigation: null };
        }

        const currentIndex = availableTasks.findIndex(task => task.id === currentTask.id);
        
        if (currentIndex === -1) {
            this.logger.warn('Cannot navigate to next task: current task not found in available tasks');
            return { success: true, navigation: null };
        }
        
        // Look for the next available task that can be opened
        for (let i = currentIndex + 1; i < availableTasks.length; i++) {
            const nextTask = availableTasks[i];
            const canOpen = await this._canOpenTask(nextTask);
            
            if (canOpen) {
                this.logger.info(`Navigating to next task: ${nextTask.id} (asset ${nextTask.assetId})`);
                return {
                    success: true,
                    navigation: {
                        projectId,
                        assetId: nextTask.assetId.toString(),
                        taskId: nextTask.id.toString()
                    }
                };
            } else {
                this.logger.debug(`Skipping next task ${nextTask.id} - status: ${nextTask.status}`);
            }
        }
        
        this.logger.info('No accessible next task found');
        return { success: true, navigation: null };
    }

    /**
     * Navigate to the previous available task that can be opened
     * @param currentTask - Current task
     * @param availableTasks - List of all available tasks
     * @param projectId - Project ID for navigation
     * @returns Promise resolving to NavigationResult
     */
    async navigateToPrevious(currentTask: Task, availableTasks: Task[], projectId: string): Promise<NavigationResult> {
        if (availableTasks.length === 0) {
            this.logger.warn('Cannot navigate to previous task: no available tasks');
            return { success: true, navigation: null };
        }

        const currentIndex = availableTasks.findIndex(task => task.id === currentTask.id);
        
        if (currentIndex === -1) {
            this.logger.warn('Cannot navigate to previous task: current task not found in available tasks');
            return { success: true, navigation: null };
        }
        
        // Look for the previous available task that can be opened
        for (let i = currentIndex - 1; i >= 0; i--) {
            const previousTask = availableTasks[i];
            const canOpen = await this._canOpenTask(previousTask);
            
            if (canOpen) {
                this.logger.info(`Navigating to previous task: ${previousTask.id} (asset ${previousTask.assetId})`);
                return {
                    success: true,
                    navigation: {
                        projectId,
                        assetId: previousTask.assetId.toString(),
                        taskId: previousTask.id.toString()
                    }
                };
            } else {
                this.logger.debug(`Skipping previous task ${previousTask.id} - status: ${previousTask.status}`);
            }
        }
        
        this.logger.info('No accessible previous task found');
        return { success: true, navigation: null };
    }

    /**
     * Get the next available uncompleted task for seamless transitions after completion
     * @param currentTask - Current task
     * @param availableTasks - List of all available tasks
     * @param projectId - Project ID for navigation
     * @returns Promise resolving to NavigationResult
     */
    async getNextAvailableTask(currentTask: Task, availableTasks: Task[], projectId: string): Promise<NavigationResult> {
        if (availableTasks.length === 0) {
            this.logger.warn('Cannot get next task: no available tasks');
            return { success: true, navigation: null };
        }

        const currentIndex = availableTasks.findIndex(task => task.id === currentTask.id);

        if (currentIndex === -1) {
            this.logger.warn('Cannot get next task: current task not found in available tasks');
            return { success: true, navigation: null };
        }

        // Look for next uncompleted task
        for (let i = currentIndex + 1; i < availableTasks.length; i++) {
            const task = availableTasks[i];
            if (!task.completedAt) {
                return {
                    success: true,
                    navigation: {
                        projectId,
                        assetId: task.assetId.toString(),
                        taskId: task.id.toString()
                    }
                };
            }
        }

        // If no task found after current, wrap around to beginning
        for (let i = 0; i < currentIndex; i++) {
            const task = availableTasks[i];
            if (!task.completedAt) {
                return {
                    success: true,
                    navigation: {
                        projectId,
                        assetId: task.assetId.toString(),
                        taskId: task.id.toString()
                    }
                };
            }
        }

        this.logger.info('No more uncompleted tasks available');
        return { success: true, navigation: null };
    }

    /**
     * Get navigation information for the current task context
     * @param currentTask - Current task
     * @param availableTasks - List of all available tasks
     * @returns NavigationInfo object with current position and navigation state
     */
    getNavigationInfo(currentTask: Task, availableTasks: Task[]): NavigationInfo {
        const currentIndex = availableTasks.findIndex(task => task.id === currentTask.id);
        
        return {
            currentIndex: currentIndex >= 0 ? currentIndex : 0,
            totalTasks: availableTasks.length,
            hasNext: currentIndex >= 0 && currentIndex < availableTasks.length - 1,
            hasPrevious: currentIndex > 0
        };
    }

    /**
     * Check if a task can be opened by the current user
     * @private
     * @param task - The task to check
     * @returns Promise resolving to boolean indicating if the task can be opened
     */
    private async _canOpenTask(task: Task): Promise<boolean> {
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
        
        // All other tasks can be opened
        return true;
    }
}