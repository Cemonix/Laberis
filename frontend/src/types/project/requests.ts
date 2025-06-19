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

export interface ProjectStatsResponse {
    totalAssets: number;
    annotatedAssets: number;
    totalAnnotations: number;
    activeAnnotators: number;
    completionPercentage: number;
}
