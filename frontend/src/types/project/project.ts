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

export interface Project {
    id: number;
    name: string;
    description: string;
    projectType: ProjectType;
    status: ProjectStatus;
    createdAt: string;
    updatedAt: string;
    ownerId?: string;
    annotationGuidelinesUrl?: string;
}

export interface CreateProjectDto {
    name: string;
    description?: string;
    projectType: ProjectType;
}

export interface UpdateProjectDto {
    name?: string;
    description?: string;
    projectType?: ProjectType;
    status?: ProjectStatus;
    annotationGuidelinesUrl?: string;
}