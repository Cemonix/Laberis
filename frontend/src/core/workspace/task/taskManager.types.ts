import type { Task, TaskStatus, PipelineResultDto } from '@/services/project/task/task.types';
import type { GetTasksResponse } from '@/services/project/task/taskService.types';

/**
 * Result types for task operations
 */
export interface TaskResult {
    success: boolean;
    task?: Task;
    message?: string;
    error?: string;
}

/**
 * Interface for task service dependency
 */
export interface TaskService {
    getTaskById(projectId: number, taskId: number): Promise<Task>;
    getTasksForAsset(projectId: number, assetId: number): Promise<Task[]>;
    getTasksForStage(projectId: number, stageId: number): Promise<GetTasksResponse>;
    completeTaskPipeline(projectId: number, taskId: number): Promise<PipelineResultDto>;
    vetoTaskPipeline(projectId: number, taskId: number, data: { reason: string }): Promise<PipelineResultDto>;
    changeTaskStatus(projectId: number, taskId: number, data: { targetStatus: TaskStatus }): Promise<Task>;
    updateWorkingTime(projectId: number, taskId: number, workingTimeMs: number): Promise<Task>;
    saveWorkingTimeBeforeUnload(projectId: number, taskId: number, workingTimeMs: number): Promise<boolean>;
}

/**
 * Interface for permissions service dependency
 */
export interface PermissionsService {
    canUpdateProject(): Promise<boolean>;
}