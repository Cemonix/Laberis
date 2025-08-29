import type { QueryParams } from '@/services/base/requests';
import type { Task, TaskStatus } from './task.types';

export interface TaskFilters {
    workflowStageId?: number;
    assignedToUserId?: string;
    status?: TaskStatus;
    priority?: number;
    isCompleted?: boolean;
}

export interface TasksQueryParams extends QueryParams {
    filterOn?: string;
    filterQuery?: string;
    sortBy?: string;
    isAscending?: boolean;
    pageNumber?: number;
    pageSize?: number;
    assetId?: string;
}

export interface GetTasksResponse {
    tasks: Task[];
    totalCount: number;
    currentPage: number;
    pageSize: number;
    totalPages: number;
}