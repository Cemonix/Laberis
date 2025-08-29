import { BaseProjectService } from '../baseProjectService';
import type { 
    TasksQueryParams,
    GetTasksResponse,
} from './taskService.types';
import { TaskStatus, type ChangeTaskStatusDto, type CompleteTaskDto, type CreateTaskRequest, type PipelineResultDto, type Task, type TaskTableRow, type TaskWithDetails, type UpdateTaskRequest, type VetoTaskDto } from './task.types';
import { workflowStageService } from '../workflow/workflowStageService';
import { assetService } from '../asset/assetService';

/**
 * Service class for managing tasks within projects.
 */
class TaskService extends BaseProjectService {
    constructor() {
        super('TaskService');
    }

    /**
     * Converts backend TaskStatus enum to frontend TaskStatus enum
     */
    private parseTaskStatus(backendStatus: string | number | undefined | null): TaskStatus {
        // Handle null, undefined, or empty values
        if (backendStatus === null || backendStatus === undefined || backendStatus === '') {
            this.logger.warn('Received null/undefined/empty status, defaulting to NOT_STARTED', { backendStatus });
            return TaskStatus.NOT_STARTED;
        }

        // Handle both string and numeric enum values from backend
        const statusStr = typeof backendStatus === 'number' ? 
            this.getTaskStatusString(backendStatus) : String(backendStatus).toUpperCase();
            
        switch (statusStr) {
            case 'NOT_STARTED': return TaskStatus.NOT_STARTED;
            case 'IN_PROGRESS': return TaskStatus.IN_PROGRESS;
            case 'COMPLETED': return TaskStatus.COMPLETED;
            case 'ARCHIVED': return TaskStatus.ARCHIVED;
            case 'SUSPENDED': return TaskStatus.SUSPENDED;
            case 'DEFERRED': return TaskStatus.DEFERRED;
            case 'READY_FOR_ANNOTATION': return TaskStatus.READY_FOR_ANNOTATION;
            case 'READY_FOR_REVIEW': return TaskStatus.READY_FOR_REVIEW;
            case 'READY_FOR_COMPLETION': return TaskStatus.READY_FOR_COMPLETION;
            case 'CHANGES_REQUIRED': return TaskStatus.CHANGES_REQUIRED;
            case 'VETOED': return TaskStatus.VETOED;
            default: 
                this.logger.warn('Unknown task status received, defaulting to NOT_STARTED', { backendStatus, statusStr });
                return TaskStatus.NOT_STARTED;
        }
    }

    /**
     * Convert numeric enum value to string (for JSON serialization compatibility)
     */
    private getTaskStatusString(enumValue: number): string {
        const statusMap: Record<number, string> = {
            0: 'NOT_STARTED',
            1: 'IN_PROGRESS',
            2: 'COMPLETED',
            3: 'ARCHIVED',
            4: 'SUSPENDED',
            5: 'DEFERRED',
            6: 'READY_FOR_ANNOTATION',
            7: 'READY_FOR_REVIEW',
            8: 'READY_FOR_COMPLETION',
            9: 'CHANGES_REQUIRED',
            10: 'VETOED'
        };
        return statusMap[enumValue] || 'NOT_STARTED';
    }

    /**
     * Transforms a task DTO from the API to a Task object
     */
    private transformTaskDto(dto: any): Task {
        const status = this.parseTaskStatus(dto.status);
        
        return {
            id: dto.id,
            priority: dto.priority,
            dueDate: dto.dueDate,
            completedAt: dto.completedAt,
            archivedAt: dto.archivedAt,
            suspendedAt: dto.suspendedAt,
            deferredAt: dto.deferredAt,
            vetoedAt: dto.vetoedAt,
            changesRequiredAt: dto.changesRequiredAt,
            workingTimeMs: dto.workingTimeMs || 0,
            createdAt: dto.createdAt,
            updatedAt: dto.updatedAt,
            assetId: dto.assetId,
            projectId: dto.projectId,
            workflowId: dto.workflowId,
            workflowStageId: dto.workflowStageId,
            assignedToEmail: dto.assignedToEmail,
            lastWorkedOnByEmail: dto.lastWorkedOnByEmail,
            status: status
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
     * Get tasks for a specific workflow stage, properly filtered by the stage's input data source.
     * This ensures only tasks for assets that belong to the stage's assigned data source are returned.
     */
    async getTasksForWorkflowStage(
        projectId: number,
        stageId: number,
        params: TasksQueryParams = {}
    ): Promise<GetTasksResponse> {
        this.logger.info(`Fetching tasks for workflow stage ${stageId} in project ${projectId}`, params);

        const url = this.buildProjectResourceUrl(projectId, 'tasks/stage/{stageId}', { stageId });
        const paginatedResponse = await this.getPaginated<any>(url, params);
        
        const tasks: Task[] = paginatedResponse.data.map((dto: any) => this.transformTaskDto(dto));

        this.logger.info(`Fetched ${tasks.length} tasks for workflow stage ${stageId}`);

        return {
            tasks,
            totalCount: paginatedResponse.totalItems,
            currentPage: paginatedResponse.currentPage,
            pageSize: paginatedResponse.pageSize,
            totalPages: paginatedResponse.totalPages
        };
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
    async getTaskById(projectId: number, taskId: number, autoAssign: boolean = true): Promise<TaskWithDetails> {
        this.logger.info(`Fetching task ${taskId} from project ${projectId}`, { autoAssign });

        const url = this.buildProjectResourceUrl(projectId, 'tasks/{taskId}', { taskId });
        const queryParams = autoAssign ? '?autoAssign=true' : '';
        const fullUrl = url + queryParams;
        const dto = await this.get<any>(fullUrl);
        
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
        const dto = await this.put<UpdateTaskRequest, any>(url, updates);
        
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

        // Get tasks for the stage (using proper data source filtering)
        const tasksResponse = await this.getTasksForWorkflowStage(projectId, stageId, params);
        
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
     * Assign a task to the current authenticated user
     */
    async assignTaskToCurrentUser(projectId: number, taskId: number): Promise<Task> {
        this.logger.info(`Assigning task ${taskId} to current user in project ${projectId}`);

        const url = this.buildProjectResourceUrl(projectId, 'tasks/{taskId}/assign-to-me', { taskId });
        const dto = await this.post<undefined, any>(url, undefined);
        
        const task: Task = this.transformTaskDto(dto);

        this.logger.info(`Assigned task ${taskId} to current user successfully`);
        return task;
    }


    /**
     * Change task status using the simplified endpoint
     */
    async changeTaskStatus(projectId: number, taskId: number, requestDto: ChangeTaskStatusDto): Promise<Task> {
        this.logger.info(`Changing task ${taskId} status to ${requestDto.targetStatus} in project ${projectId}`);

        const url = this.buildProjectResourceUrl(projectId, 'tasks/{taskId}/status', { taskId });
        const dto = await this.put<ChangeTaskStatusDto, any>(url, requestDto);
        
        const task: Task = this.transformTaskDto(dto);

        this.logger.info(`Changed task ${taskId} status to ${requestDto.targetStatus} successfully`);
        return task;
    }

    /**
     * Transfer an asset to a different data source
     */
    async transferAsset(projectId: number, assetId: number, targetDataSourceId: number): Promise<void> {
        this.logger.info(`Transferring asset ${assetId} to data source ${targetDataSourceId} in project ${projectId}`);

        const url = this.buildProjectResourceUrl(projectId, 'assets/{assetId}/transfer', { assetId });
        const transferDto = { targetDataSourceId };
        
        await this.post<typeof transferDto, any>(url, transferDto);

        this.logger.info(`Successfully transferred asset ${assetId} to data source ${targetDataSourceId}`);
    }

    /**
     * Create a new task for an asset in a specific workflow stage
     */
    async createTaskForAsset(projectId: number, assetId: number, workflowId: number, stageId: number, priority: number = 5): Promise<Task> {
        this.logger.info(`Creating task for asset ${assetId} in stage ${stageId}, project ${projectId}`);

        const url = this.buildProjectUrl(projectId, 'tasks');
        const createTaskDto = {
            priority,
            assetId,
            workflowId,
            workflowStageId: stageId
        };
        
        const dto = await this.post<typeof createTaskDto, any>(url, createTaskDto);
        const task: Task = this.transformTaskDto(dto);

        this.logger.info(`Created task ${task.id} for asset ${assetId} successfully`);
        return task;
    }

    /**
     * Update working time for a task
     * This is a specialized method for updating only the working time field
     */
    async updateWorkingTime(projectId: number, taskId: number, workingTimeMs: number): Promise<Task> {
        this.logger.info(`Updating working time for task ${taskId} in project ${projectId}: ${workingTimeMs}ms`);
        
        const updates: UpdateTaskRequest = {
            workingTimeMs: workingTimeMs
        };
        
        return this.updateTask(projectId, taskId, updates);
    }

    /**
     * Save working time before page unload (optimized for beforeunload events)
     * Uses the existing updateTask method but with optimized logging for page unload scenarios
     */
    async saveWorkingTimeBeforeUnload(projectId: number, taskId: number, workingTimeMs: number): Promise<boolean> {
        try {
            this.logger.info(`Saving working time before unload for task ${taskId}: ${workingTimeMs}ms`);
            
            await this.updateWorkingTime(projectId, taskId, workingTimeMs);
            
            this.logger.info(`Successfully saved working time before unload: ${workingTimeMs}ms`);
            return true;
        } catch (error) {
            this.logger.warn('Failed to save working time before unload:', error);
            // Don't throw error - this is a background operation during page unload
            return false;
        }
    }

    /**
     * Complete a task using the pipeline system
     * This triggers the complete workflow progression including asset transfer and next stage task creation
     */
    async completeTaskPipeline(projectId: number, taskId: number, dto: CompleteTaskDto = {}): Promise<PipelineResultDto> {
        this.logger.info(`Completing task ${taskId} using pipeline in project ${projectId}`, dto);

        const url = this.buildProjectResourceUrl(projectId, 'tasks/{taskId}/complete', { taskId });
        const result = await this.post<CompleteTaskDto, PipelineResultDto>(url, dto);

        this.logger.info(`Task completion pipeline result for task ${taskId}:`, result);
        return result;
    }

    /**
     * Veto a task using the pipeline system
     * This triggers the veto workflow progression including asset transfer back to annotation and task updates
     */
    async vetoTaskPipeline(projectId: number, taskId: number, dto: VetoTaskDto): Promise<PipelineResultDto> {
        this.logger.info(`Vetoing task ${taskId} using pipeline in project ${projectId}`, dto);

        const url = this.buildProjectResourceUrl(projectId, 'tasks/{taskId}/veto', { taskId });
        const result = await this.post<VetoTaskDto, PipelineResultDto>(url, dto);

        this.logger.info(`Task veto pipeline result for task ${taskId}:`, result);
        return result;
    }

    /**
     * Check if the current user can complete the specified task
     */
    async canCompleteTask(projectId: number, taskId: number): Promise<boolean> {
        this.logger.debug(`Checking completion permissions for task ${taskId} in project ${projectId}`);

        const url = this.buildProjectResourceUrl(projectId, 'tasks/{taskId}/can-complete', { taskId });
        const canComplete = await this.get<boolean>(url);

        this.logger.debug(`Can complete task ${taskId}: ${canComplete}`);
        return canComplete;
    }

    /**
     * Check if the current user can veto the specified task
     */
    async canVetoTask(projectId: number, taskId: number): Promise<boolean> {
        this.logger.debug(`Checking veto permissions for task ${taskId} in project ${projectId}`);

        const url = this.buildProjectResourceUrl(projectId, 'tasks/{taskId}/can-veto', { taskId });
        const canVeto = await this.get<boolean>(url);

        this.logger.debug(`Can veto task ${taskId}: ${canVeto}`);
        return canVeto;
    }
}

export const taskService = new TaskService();