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

/**
 * Service class for managing tasks within projects.
 * Extends BaseProjectService to inherit common project-related functionality.
 */
class TaskService extends BaseProjectService {
    constructor() {
        super('TaskService');
    }

    /**
     * Derives task status from completion and archive state
     */
    private deriveStatus(completedAt: string | null, archivedAt: string | null, assignedToUser: string | null): TaskStatus {
        if (archivedAt) return TaskStatus.ARCHIVED;
        if (completedAt) return TaskStatus.COMPLETED;
        if (assignedToUser) return TaskStatus.IN_PROGRESS;
        return TaskStatus.NOT_STARTED;
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
        const response = await this.getPaginated<any>(url, params);
        
        // Transform DTOs to Task objects
        const tasks: Task[] = response.data.map((dto: any) => ({
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
        }));

        this.logger.info(`Fetched ${tasks.length} tasks for project ${projectId}`);

        return {
            tasks,
            totalCount: response.totalItems,
            currentPage: response.currentPage,
            pageSize: response.pageSize,
            totalPages: response.totalPages
        };
    }

    /**
     * Get a single task by ID with detailed information
     */
    async getTaskById(projectId: number, taskId: number): Promise<TaskWithDetails> {
        this.logger.info(`Fetching task ${taskId} from project ${projectId}`);

        const url = this.buildProjectUrl(projectId, `tasks/${taskId}`);
        const response = await this.get<TaskWithDetails>(url);

        this.logger.info(`Fetched task ${taskId} successfully`);
        return response;
    }

    /**
     * Create a new task in a project
     */
    async createTask(projectId: number, taskData: CreateTaskRequest): Promise<Task> {
        this.logger.info(`Creating task in project ${projectId}`, taskData);

        const url = this.buildProjectUrl(projectId, 'tasks');
        const response = await this.post<CreateTaskRequest, Task>(url, taskData);

        this.logger.info(`Created task ${response.id} in project ${projectId}`);
        return response;
    }

    /**
     * Update an existing task
     */
    async updateTask(projectId: number, taskId: number, taskData: UpdateTaskRequest): Promise<Task> {
        this.logger.info(`Updating task ${taskId} in project ${projectId}`, taskData);

        const url = this.buildProjectUrl(projectId, `tasks/${taskId}`);
        const response = await this.put<UpdateTaskRequest, Task>(url, taskData);

        this.logger.info(`Updated task ${taskId} successfully`);
        return response;
    }

    /**
     * Delete a task
     */
    async deleteTask(projectId: number, taskId: number): Promise<void> {
        this.logger.info(`Deleting task ${taskId} from project ${projectId}`);

        const url = this.buildProjectUrl(projectId, `tasks/${taskId}`);
        await this.delete(url);

        this.logger.info(`Deleted task ${taskId} successfully`);
    }

    /**
     * Get task table data with asset information
     */
    async getTaskTableData(projectId: number, params: TasksQueryParams = {}): Promise<TaskTableRow[]> {
        this.logger.info(`Fetching task table data for project ${projectId}`, params);

        const url = this.buildProjectUrl(projectId, 'tasks/table');
        const response = await this.getPaginated<TaskTableRow>(url, params);

        this.logger.info(`Fetched ${response.data.length} task table rows`);
        return response.data;
    }

    /**
     * Get tasks by workflow stage
     */
    async getTasksByWorkflowStage(
        projectId: number,
        workflowStageId: number,
        params: TasksQueryParams = {}
    ): Promise<GetTasksResponse> {
        this.logger.info(`Fetching tasks for workflow stage ${workflowStageId} in project ${projectId}`, params);

        const queryParams = { ...params, workflowStageId: workflowStageId.toString() };
        const url = this.buildProjectUrl(projectId, 'tasks');
        const response = await this.getPaginated<any>(url, queryParams);
        
        const tasks: Task[] = response.data.map((dto: any) => ({
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
        }));

        this.logger.info(`Fetched ${tasks.length} tasks for workflow stage ${workflowStageId}`);

        return {
            tasks,
            totalCount: response.totalItems,
            currentPage: response.currentPage,
            pageSize: response.pageSize,
            totalPages: response.totalPages
        };
    }

    /**
     * Get tasks for a specific asset within a project
     */
    async getTasksForAsset(
        projectId: number,
        assetId: number,
        params: TasksQueryParams = {}
    ): Promise<GetTasksResponse> {
        this.logger.info(`Fetching tasks for asset ${assetId} in project ${projectId}`, params);

        // Filter tasks by assetId
        const queryParams = { ...params, assetId: assetId.toString() };
        const url = this.buildProjectUrl(projectId, 'tasks');
        const response = await this.getPaginated<any>(url, queryParams);
        
        const tasks: Task[] = response.data.map((dto: any) => ({
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
        }));

        this.logger.info(`Fetched ${tasks.length} tasks for asset ${assetId}`);

        return {
            tasks,
            totalCount: response.totalItems,
            currentPage: response.currentPage,
            pageSize: response.pageSize,
            totalPages: response.totalPages
        };
    }
}

export const taskService = new TaskService();