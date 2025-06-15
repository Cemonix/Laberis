import type { ProjectType, ProjectStatus } from './project';

export interface CreateProjectRequest {
    name: string;
    description?: string;
    projectType: ProjectType;
}

export interface UpdateProjectRequest {
    name: string;
    description?: string;
    status: ProjectStatus;
    annotationGuidelinesUrl?: string;
}

export interface ProjectListParams {
    page?: number;
    pageSize?: number;
    status?: ProjectStatus;
    projectType?: ProjectType;
    search?: string;
    sortBy?: 'name' | 'createdAt' | 'updatedAt';
    sortOrder?: 'asc' | 'desc';
}

export interface ProjectStatsResponse {
    totalAssets: number;
    annotatedAssets: number;
    totalAnnotations: number;
    activeAnnotators: number;
    completionPercentage: number;
}
