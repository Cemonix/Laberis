import { BaseProjectService } from '../base';
import type { 
    Task, 
    TaskWithDetails, 
    CreateTaskRequest, 
    UpdateTaskRequest,
    TaskTableRow,
    TasksQueryParams,
    GetTasksResponse
} from '@/types/task';
import { TaskStatus } from '@/types/task';
import { workflowStageService } from '../workflows';
import { assetService } from './assetService';

/**
 * Service class for managing tasks within projects.
 */
class TaskService extends BaseProjectService {
    constructor() {
        super('TaskService');
    }

    /**
     * Derives task status from completion and archive state
     */
    private deriveStatus(
        completedAt?: string | null, 
        archivedAt?: string | null, 
        assignedToEmail?: string | null
    ): TaskStatus {
        if (archivedAt) return TaskStatus.ARCHIVED;
        if (completedAt) return TaskStatus.COMPLETED;
        if (assignedToEmail) return TaskStatus.IN_PROGRESS;
        return TaskStatus.NOT_STARTED;
    }

    /**
     * Transforms a task DTO from the API to a Task object
     */
    private transformTaskDto(dto: any): Task {
        return {
            id: dto.id,
            priority: dto.priority,
            dueDate: dto.dueDate,
            completedAt: dto.completedAt,
            archivedAt: dto.archivedAt,
            createdAt: dto.createdAt,
            updatedAt: dto.updatedAt,
            assetId: dto.assetId,
            projectId: dto.projectId,
            workflowId: dto.workflowId,
            currentWorkflowStageId: dto.currentWorkflowStageId,
            assignedToEmail: dto.assignedToEmail,
            lastWorkedOnByEmail: dto.lastWorkedOnByEmail,
            status: this.deriveStatus(dto.completedAt, dto.archivedAt, dto.assignedToEmail)
        };
    }

    /**
     * Get all tasks for a specific project with optional filtering and pagination
     */
    async getTasksForProject(
        projectId: number,
        params: TasksQueryParams = {}
    ): Promise<GetTasksResponse> {
        this.logger.info(`Fetching tasks for project ${projectId}`, params);

        const url = this.buildProjectUrl(projectId, 'tasks');
        const paginatedResponse = await this.getPaginated<any>(url, params);
        
        const tasks: Task[] = paginatedResponse.data.map((dto: any) => this.transformTaskDto(dto));

        this.logger.info(`Fetched ${tasks.length} tasks for project ${projectId}`);

        return {
            tasks,
            totalCount: paginatedResponse.totalItems,
            currentPage: paginatedResponse.currentPage,
            pageSize: paginatedResponse.pageSize,
            totalPages: paginatedResponse.totalPages
        };
    }

    /**
     * Get tasks for a specific workflow stage
     */
    async getTasksForStage(
        projectId: number,
        stageId: number,
        params: TasksQueryParams = {}
    ): Promise<GetTasksResponse> {
        const stageParams = {
            ...params,
            filterOn: 'current_workflow_stage_id',
            filterQuery: stageId.toString()
        };
        
        return this.getTasksForProject(projectId, stageParams);
    }

    /**
     * Get current user's tasks
     */
    async getMyTasks(params: TasksQueryParams = {}): Promise<GetTasksResponse> {
        this.logger.info('Fetching current user tasks', params);

        const url = this.getBaseUrl('tasks/my-tasks');
        const paginatedResponse = await this.getPaginated<any>(url, params);
        
        const tasks: Task[] = paginatedResponse.data.map((dto: any) => this.transformTaskDto(dto));

        this.logger.info(`Fetched ${tasks.length} user tasks`);

        return {
            tasks,
            totalCount: paginatedResponse.totalItems,
            currentPage: paginatedResponse.currentPage,
            pageSize: paginatedResponse.pageSize,
            totalPages: paginatedResponse.totalPages
        };
    }

    /**
     * Get tasks for a specific asset
     */
    async getTasksForAsset(projectId: number, assetId: number): Promise<Task[]> {
        this.logger.info(`Fetching tasks for asset ${assetId} in project ${projectId}`);

        const params: TasksQueryParams = {
            filterOn: 'asset_id',
            filterQuery: assetId.toString()
        };
        
        const response = await this.getTasksForProject(projectId, params);
        return response.tasks;
    }

    /**
     * Get a single task by ID
     */
    async getTaskById(projectId: number, taskId: number): Promise<TaskWithDetails> {
        this.logger.info(`Fetching task ${taskId} from project ${projectId}`);

        const url = this.buildProjectResourceUrl(projectId, 'tasks/{taskId}', { taskId });
        const dto = await this.get<any>(url);
        
        const task: TaskWithDetails = this.transformTaskDto(dto) as TaskWithDetails;

        this.logger.info(`Fetched task ${taskId} successfully`);
        return task;
    }

    /**
     * Create a new task
     */
    async createTask(projectId: number, taskData: CreateTaskRequest): Promise<Task> {
        this.logger.info(`Creating task in project ${projectId}`, taskData);

        const url = this.buildProjectUrl(projectId, 'tasks');
        const dto = await this.post<CreateTaskRequest, any>(url, taskData);
        
        const task: Task = this.transformTaskDto(dto);

        this.logger.info(`Created task ${task.id} in project ${projectId}`);
        return task;
    }

    /**
     * Update an existing task
     */
    async updateTask(projectId: number, taskId: number, updates: UpdateTaskRequest): Promise<Task> {
        this.logger.info(`Updating task ${taskId} in project ${projectId}`, updates);

        const url = this.buildProjectResourceUrl(projectId, 'tasks/{taskId}', { taskId });
        const dto = await this.patch<UpdateTaskRequest, any>(url, updates);
        
        const task: Task = this.transformTaskDto(dto);

        this.logger.info(`Updated task ${taskId} successfully`);
        return task;
    }

    /**
     * Assign a task to a user
     */
    async assignTask(projectId: number, taskId: number, userEmail: string): Promise<Task> {
        this.logger.info(`Assigning task ${taskId} to ${userEmail} in project ${projectId}`);

        const url = this.buildProjectResourceUrl(projectId, 'tasks/{taskId}/assign', { taskId });
        const dto = await this.post<{ userEmail: string }, any>(url, { userEmail });
        
        const task: Task = this.transformTaskDto(dto);

        this.logger.info(`Assigned task ${taskId} to ${userEmail} successfully`);
        return task;
    }

    /**
     * Move a task to a different workflow stage
     */
    async moveTaskToStage(projectId: number, taskId: number, stageId: number): Promise<Task> {
        this.logger.info(`Moving task ${taskId} to stage ${stageId} in project ${projectId}`);

        const url = this.buildProjectResourceUrl(projectId, 'tasks/{taskId}/move-to-stage/{stageId}', { taskId, stageId });
        const dto = await this.post<undefined, any>(url, undefined);
        
        const task: Task = this.transformTaskDto(dto);

        this.logger.info(`Moved task ${taskId} to stage ${stageId} successfully`);
        return task;
    }

    /**
     * Delete a task
     */
    async deleteTask(projectId: number, taskId: number): Promise<void> {
        this.logger.info(`Deleting task ${taskId} from project ${projectId}`);

        const url = this.buildProjectResourceUrl(projectId, 'tasks/{taskId}', { taskId });
        await this.delete(url);

        this.logger.info(`Deleted task ${taskId} successfully`);
    }

    /**
     * Get enriched tasks for a workflow stage with asset names and stage info
     */
    async getEnrichedTasksForStage(
        projectId: number,
        workflowId: number,
        stageId: number,
        params: TasksQueryParams = {}
    ): Promise<{
        tasks: TaskTableRow[];
        stageName: string;
        stageDescription?: string;
        totalCount: number;
        currentPage: number;
        pageSize: number;
        totalPages: number;
    }> {
        this.logger.info(`Fetching enriched tasks for stage ${stageId} in workflow ${workflowId}, project ${projectId}`, params);

        // Get tasks for the stage
        const tasksResponse = await this.getTasksForStage(projectId, stageId, params);
        
        // Get stage information
        let stageName = 'Unknown Stage';
        let stageDescription = '';
        
        try {
            const stageData = await workflowStageService.getWorkflowStageById(projectId, workflowId, stageId);
            stageName = stageData.name;
            stageDescription = stageData.description || '';
        } catch (error) {
            this.logger.warn('Failed to fetch stage information:', error);
        }

        // Enrich tasks with asset names and format for table display
        const enrichedTasks: TaskTableRow[] = await Promise.all(
            tasksResponse.tasks.map(async (task) => {
                let assetName = `Asset ${task.assetId}`;
                
                try {
                    const asset = await assetService.getAssetById(projectId, task.assetId);
                    assetName = asset.filename || `Asset ${task.assetId}`;
                } catch (error) {
                    this.logger.warn(`Failed to fetch asset ${task.assetId}:`, error);
                }

                return {
                    id: task.id,
                    assetId: task.assetId,
                    assetName,
                    priority: task.priority,
                    status: task.status || TaskStatus.NOT_STARTED,
                    assignedTo: task.assignedToEmail || undefined,
                    dueDate: task.dueDate,
                    completedAt: task.completedAt,
                    createdAt: task.createdAt,
                    stage: stageName
                };
            })
        );

        this.logger.info(`Fetched ${enrichedTasks.length} enriched tasks for stage ${stageId}`);

        return {
            tasks: enrichedTasks,
            stageName,
            stageDescription,
            totalCount: tasksResponse.totalCount,
            currentPage: tasksResponse.currentPage,
            pageSize: tasksResponse.pageSize,
            totalPages: tasksResponse.totalPages
        };
    }

    /**
     * Complete a task (marks as completed but doesn't move to next stage)
     */
    async completeTask(projectId: number, taskId: number): Promise<Task> {
        this.logger.info(`Completing task ${taskId} in project ${projectId}`);

        const url = this.buildProjectResourceUrl(projectId, 'tasks/{taskId}/complete', { taskId });
        const dto = await this.put<undefined, any>(url, undefined);
        
        const task: Task = this.transformTaskDto(dto);

        this.logger.info(`Completed task ${taskId} successfully`);
        return task;
    }

    /**
     * Complete a task and automatically move it to the next workflow stage
     */
    async completeAndMoveTask(projectId: number, taskId: number): Promise<Task> {
        this.logger.info(`Completing and moving task ${taskId} to next stage in project ${projectId}`);

        const url = this.buildProjectResourceUrl(projectId, 'tasks/{taskId}/complete-and-move', { taskId });
        const dto = await this.put<undefined, any>(url, undefined);
        
        const task: Task = this.transformTaskDto(dto);

        this.logger.info(`Completed and moved task ${taskId} to next stage successfully`);
        return task;
    }

    /**
     * Assign a task to a user by user ID
     */
    async assignTaskByUserId(projectId: number, taskId: number, userId: string): Promise<Task> {
        this.logger.info(`Assigning task ${taskId} to user ${userId} in project ${projectId}`);

        const url = this.buildProjectResourceUrl(projectId, 'tasks/{taskId}/assign/{userId}', { taskId, userId });
        const dto = await this.post<undefined, any>(url, undefined);
        
        const task: Task = this.transformTaskDto(dto);

        this.logger.info(`Assigned task ${taskId} to user ${userId} successfully`);
        return task;
    }

    /**
     * Mark a completed task as uncomplete
     */
    async uncompleteTask(projectId: number, taskId: number): Promise<Task> {
        this.logger.info(`Uncompleting task ${taskId} in project ${projectId}`);

        const url = this.buildProjectResourceUrl(projectId, 'tasks/{taskId}/incomplete', { taskId });
        const dto = await this.put<undefined, any>(url, undefined);
        
        const task: Task = this.transformTaskDto(dto);

        this.logger.info(`Uncompleted task ${taskId} successfully`);
        return task;
    }

    /**
     * Suspend a task (mark as suspended)
     */
    async suspendTask(projectId: number, taskId: number): Promise<Task> {
        this.logger.info(`Suspending task ${taskId} in project ${projectId}`);

        const url = this.buildProjectResourceUrl(projectId, 'tasks/{taskId}/suspend', { taskId });
        const dto = await this.put<undefined, any>(url, undefined);
        
        const task: Task = this.transformTaskDto(dto);

        this.logger.info(`Suspended task ${taskId} successfully`);
        return task;
    }

    /**
     * Check if there are assets available for task creation in a project
     */
    async checkAssetsAvailable(projectId: number): Promise<{ 
        hasAssets: boolean; 
        count: number; 
    }> {
        this.logger.info(`Checking asset availability for project ${projectId}`);

        try {
            const url = this.buildProjectUrl(projectId, 'tasks/available-assets-count');
            const data = await this.get<{ count: number }>(url);
            
            const hasAssets = data.count > 0;
            const count = data.count;
            
            this.logger.info(`Project ${projectId} has ${count} assets available`);
            
            return {
                hasAssets,
                count
            };
        } catch (error) {
            this.logger.warn('Failed to check assets availability:', error);
            return { hasAssets: false, count: 0 };
        }
    }
}

export const taskService = new TaskService();