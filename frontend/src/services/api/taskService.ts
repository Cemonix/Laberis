import apiClient from './apiClient';
import type { 
    Task, 
    TaskWithDetails, 
    CreateTaskRequest, 
    UpdateTaskRequest,
    TaskTableRow
} from '@/types/task';
import { TaskStatus } from '@/types/task';
import assetService from './assetService';
import { workflowStageService } from './workflowStageService';

export interface TaskFilters {
    workflowStageId?: number;
    assignedToUserId?: string;
    status?: TaskStatus;
    priority?: number;
    isCompleted?: boolean;
}

export interface TasksQueryParams {
    filterOn?: string;
    filterQuery?: string;
    sortBy?: string;
    isAscending?: boolean;
    pageNumber?: number;
    pageSize?: number;
}

export interface GetTasksResponse {
    tasks: Task[];
    totalCount: number;
    currentPage: number;
    pageSize: number;
    totalPages: number;
}

class TaskService {
    private readonly baseUrl = 'projects';

    /**
     * Get all tasks for a specific project with optional filtering and pagination
     */
    async getTasksForProject(
        projectId: number,
        params: TasksQueryParams = {}
    ): Promise<GetTasksResponse> {
        const queryParams = new URLSearchParams();
        
        if (params.filterOn) queryParams.append('filterOn', params.filterOn);
        if (params.filterQuery) queryParams.append('filterQuery', params.filterQuery);
        if (params.sortBy) queryParams.append('sortBy', params.sortBy);
        if (params.isAscending !== undefined) queryParams.append('isAscending', params.isAscending.toString());
        if (params.pageNumber) queryParams.append('pageNumber', params.pageNumber.toString());
        if (params.pageSize) queryParams.append('pageSize', params.pageSize.toString());

        const url = `${this.baseUrl}/${projectId}/tasks?${queryParams.toString()}`;
        const response = await apiClient.get(url);
        
        // Transform backend DTO to frontend interface
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
            // Derive status from completion/archive state
            status: this.deriveStatus(dto.completedAt, dto.archivedAt, dto.assignedToEmail)
        }));

        // For now, return paginated response format
        // TODO: Update when backend returns pagination metadata
        return {
            tasks,
            totalCount: tasks.length,
            currentPage: params.pageNumber || 1,
            pageSize: params.pageSize || 25,
            totalPages: Math.ceil(tasks.length / (params.pageSize || 25))
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
        const queryParams = new URLSearchParams();
        
        if (params.filterOn) queryParams.append('filterOn', params.filterOn);
        if (params.filterQuery) queryParams.append('filterQuery', params.filterQuery);
        if (params.sortBy) queryParams.append('sortBy', params.sortBy);
        if (params.isAscending !== undefined) queryParams.append('isAscending', params.isAscending.toString());
        if (params.pageNumber) queryParams.append('pageNumber', params.pageNumber.toString());
        if (params.pageSize) queryParams.append('pageSize', params.pageSize.toString());

        const url = `tasks/my-tasks?${queryParams.toString()}`;
        const response = await apiClient.get(url);
        
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

        return {
            tasks,
            totalCount: tasks.length,
            currentPage: params.pageNumber || 1,
            pageSize: params.pageSize || 25,
            totalPages: Math.ceil(tasks.length / (params.pageSize || 25))
        };
    }

    /**
     * Get a single task by ID
     */
    async getTaskById(projectId: number, taskId: number): Promise<TaskWithDetails> {
        const url = `${this.baseUrl}/${projectId}/tasks/${taskId}`;
        const response = await apiClient.get(url);
        
        const dto = response.data;
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
     * Create a new task
     */
    async createTask(projectId: number, taskData: CreateTaskRequest): Promise<Task> {
        const url = `${this.baseUrl}/${projectId}/tasks`;
        const response = await apiClient.post(url, taskData);
        
        const dto = response.data;
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
     * Update an existing task
     */
    async updateTask(projectId: number, taskId: number, updates: UpdateTaskRequest): Promise<Task> {
        const url = `${this.baseUrl}/${projectId}/tasks/${taskId}`;
        const response = await apiClient.patch(url, updates);
        
        const dto = response.data;
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
     * Assign a task to a user
     */
    async assignTask(projectId: number, taskId: number, userEmail: string): Promise<Task> {
        const url = `${this.baseUrl}/${projectId}/tasks/${taskId}/assign`;
        const response = await apiClient.post(url, { userEmail });
        
        const dto = response.data;
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
     * Move a task to a different workflow stage
     */
    async moveTaskToStage(projectId: number, taskId: number, stageId: number): Promise<Task> {
        const url = `${this.baseUrl}/${projectId}/tasks/${taskId}/move-to-stage/${stageId}`;
        const response = await apiClient.post(url);
        
        const dto = response.data;
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
     * Delete a task
     */
    async deleteTask(projectId: number, taskId: number): Promise<void> {
        const url = `${this.baseUrl}/${projectId}/tasks/${taskId}`;
        await apiClient.delete(url);
    }

    /**
     * Derive task status from completion and archive dates
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
        // Get tasks for the stage
        const tasksResponse = await this.getTasksForStage(projectId, stageId, params);
        
        // Get stage information
        let stageName = 'Unknown Stage';
        let stageDescription = '';
        
        try {
            const stageData = await workflowStageService.getWorkflowStage(projectId, workflowId, stageId);
            stageName = stageData.name;
            stageDescription = stageData.description || '';
        } catch (error) {
            console.warn('Failed to fetch stage information:', error);
        }

        // Enrich tasks with asset names and format for table display
        const enrichedTasks: TaskTableRow[] = await Promise.all(
            tasksResponse.tasks.map(async (task) => {
                let assetName = `Asset ${task.assetId}`;
                
                try {
                    const asset = await assetService.getAssetById(projectId, task.assetId);
                    assetName = asset.filename || `Asset ${task.assetId}`;
                } catch (error) {
                    console.warn(`Failed to fetch asset ${task.assetId}:`, error);
                }

                return {
                    id: task.id,
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
}

export const taskService = new TaskService();
