import type { QueryParams } from '@/types/api';
import type { Task } from '@/types/task';
import { TaskStatus } from '@/types/task';

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