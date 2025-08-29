// Domain enums - should match backend exactly
export enum ProjectType {
    OTHER = 'OTHER',
    IMAGE_CLASSIFICATION = 'IMAGE_CLASSIFICATION',
    OBJECT_DETECTION = 'OBJECT_DETECTION',
    IMAGE_SEGMENTATION = 'IMAGE_SEGMENTATION',
    VIDEO_ANNOTATION = 'VIDEO_ANNOTATION',
    TEXT_ANNOTATION = 'TEXT_ANNOTATION',
}

export enum ProjectStatus {
    ACTIVE = 'ACTIVE',
    ARCHIVED = 'ARCHIVED',
    READ_ONLY = 'READ_ONLY',
    PENDING_DELETION = 'PENDING_DELETION',
}

export enum ProjectRole {
    MANAGER = 'MANAGER',
    ANNOTATOR = 'ANNOTATOR',
    REVIEWER = 'REVIEWER',
    VIEWER = 'VIEWER'
}

// Main Project interface
export interface Project {
    id: number;
    name: string;
    description: string;
    projectType: ProjectType;
    status: ProjectStatus;
    createdAt: string;
    updatedAt: string;
    ownerEmail?: string;
    annotationGuidelinesUrl?: string;
}

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