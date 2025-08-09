import { taskService } from './api/projects';
import type {
    TaskBulkOperationsService,
    BulkOperationResult,
    BulkOperationOptions,
    BulkPriorityUpdateRequest,
    BulkAssignmentRequest,
    BulkArchiveRequest,
    BulkOperationError
} from '@/types/task';
import { AppLogger } from '@/utils/logger';

/**
 * Service for performing bulk operations on tasks
 * Implements the TaskBulkOperationsService interface
 */
class TaskBulkOperations implements TaskBulkOperationsService {
    private logger = AppLogger.createServiceLogger('TaskBulkOperations');
    
    private readonly DEFAULT_OPTIONS: Required<BulkOperationOptions> = {
        continueOnError: true,
        batchSize: 10,
        onProgress: () => {} // no-op
    };

    /**
     * Update priority for multiple tasks
     */
    async bulkUpdatePriority(
        projectId: number,
        request: BulkPriorityUpdateRequest,
        options: BulkOperationOptions = {}
    ): Promise<BulkOperationResult> {
        const opts = { ...this.DEFAULT_OPTIONS, ...options };
        
        this.logger.info('Starting bulk priority update', {
            projectId,
            taskCount: request.taskIds.length,
            priority: request.priority,
            batchSize: opts.batchSize
        });

        return this.processBulkOperation(
            request.taskIds,
            async (taskId) => {
                // Update only priority - don't touch assignment fields
                await taskService.updateTask(projectId, taskId, {
                    priority: request.priority
                    // Deliberately omit assignedToEmail to avoid unintended assignment changes
                });
                return taskId;
            },
            opts,
            'priority update'
        );
    }

    /**
     * Assign multiple tasks to a user
     */
    async bulkAssignTasks(
        projectId: number,
        request: BulkAssignmentRequest,
        options: BulkOperationOptions = {}
    ): Promise<BulkOperationResult> {
        const opts = { ...this.DEFAULT_OPTIONS, ...options };
        
        this.logger.info('Starting bulk task assignment', {
            projectId,
            taskCount: request.taskIds.length,
            assigneeEmail: request.assigneeEmail,
            batchSize: opts.batchSize
        });

        return this.processBulkOperation(
            request.taskIds,
            async (taskId) => {
                await taskService.updateTask(projectId, taskId, {
                    assignedToEmail: request.assigneeEmail
                });
                return taskId;
            },
            opts,
            'assignment'
        );
    }

    /**
     * Archive multiple tasks
     */
    async bulkArchiveTasks(
        projectId: number,
        request: BulkArchiveRequest,
        options: BulkOperationOptions = {}
    ): Promise<BulkOperationResult> {
        const opts = { ...this.DEFAULT_OPTIONS, ...options };
        
        this.logger.info('Starting bulk task archival', {
            projectId,
            taskCount: request.taskIds.length,
            reason: request.reason,
            batchSize: opts.batchSize
        });

        return this.processBulkOperation(
            request.taskIds,
            async (taskId) => {
                // Use the task status change API to archive tasks
                await taskService.changeTaskStatus(projectId, taskId, {
                    targetStatus: 'ARCHIVED' as any, // TODO: Fix this when TaskStatus enum is properly imported
                    moveAsset: false
                });
                return taskId;
            },
            opts,
            'archive'
        );
    }

    /**
     * Generic method to process bulk operations with batching and error handling
     */
    private async processBulkOperation<T>(
        items: T[],
        operation: (item: T) => Promise<T>,
        options: Required<BulkOperationOptions>,
        operationName: string
    ): Promise<BulkOperationResult> {
        const succeeded: number[] = [];
        const failed: BulkOperationError[] = [];
        const total = items.length;
        let processed = 0;

        // Process in batches
        for (let i = 0; i < items.length; i += options.batchSize) {
            const batch = items.slice(i, i + options.batchSize);
            
            // Process batch items in parallel
            const batchPromises = batch.map(async (item) => {
                try {
                    const result = await operation(item);
                    succeeded.push(result as number);
                    processed++;
                    options.onProgress(processed, total);
                    return { success: true, item, result };
                } catch (error) {
                    const errorInfo: BulkOperationError = {
                        taskId: item as number,
                        error: error instanceof Error ? error.message : String(error),
                        code: this.extractErrorCode(error)
                    };
                    failed.push(errorInfo);
                    processed++;
                    options.onProgress(processed, total);
                    
                    this.logger.warn(`${operationName} failed for item`, {
                        item,
                        error: errorInfo.error
                    });
                    
                    return { success: false, item, error: errorInfo };
                }
            });

            // Wait for batch to complete
            await Promise.all(batchPromises);

            // If continueOnError is false and we have failures, stop processing
            if (!options.continueOnError && failed.length > 0) {
                this.logger.warn(`Stopping ${operationName} due to errors`, {
                    failedCount: failed.length,
                    processed: succeeded.length
                });
                break;
            }
        }

        const result: BulkOperationResult = {
            succeeded,
            failed,
            total,
            successRate: total > 0 ? succeeded.length / total : 1
        };

        this.logger.info(`Bulk ${operationName} completed`, {
            succeeded: succeeded.length,
            failed: failed.length,
            total,
            successRate: result.successRate
        });

        return result;
    }

    /**
     * Extract error code from various error types
     */
    private extractErrorCode(error: unknown): string | undefined {
        if (error && typeof error === 'object') {
            const errorObj = error as any;
            
            // Check for common error code properties
            if ('code' in errorObj) return errorObj.code;
            if ('status' in errorObj) return String(errorObj.status);
            if ('statusCode' in errorObj) return String(errorObj.statusCode);
            
            // Check for HTTP response errors
            if ('response' in errorObj && errorObj.response?.status) {
                return String(errorObj.response.status);
            }
        }
        
        return undefined;
    }


    /**
     * Get operation status summary for UI display
     */
    getOperationSummary(result: BulkOperationResult): string {
        const { succeeded, failed, total } = result;
        
        if (failed.length === 0) {
            return `Successfully processed ${succeeded.length} task${succeeded.length === 1 ? '' : 's'}`;
        } else if (succeeded.length === 0) {
            return `Failed to process all ${total} tasks`;
        } else {
            return `Processed ${succeeded.length} task${succeeded.length === 1 ? '' : 's'}, ${failed.length} failed`;
        }
    }
}

// Export singleton instance
export const taskBulkOperations = new TaskBulkOperations();