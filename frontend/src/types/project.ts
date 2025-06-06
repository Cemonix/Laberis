export enum ProjectType {
    OTHER = 'other',
    IMAGE_CLASSIFICATION = 'image_classification',
    OBJECT_DETECTION = 'object_detection',
    IMAGE_SEGMENTATION = 'image_segmentation',
    VIDEO_ANNOTATION = 'video_annotation',
    TEXT_ANNOTATION = 'text_annotation',
}

export enum ProjectStatus {
    ACTIVE = 'active',
    ARCHIVED = 'archived',
    READ_ONLY = 'read_only',
    PENDING_DELETION = 'pending_deletion',
}

export interface Project {
    projectId: number;
    name: string;
    description?: string;
    projectType: ProjectType;
    status: ProjectStatus;
    createdAt: string;
    updatedAt: string;
    
    assetCount?: number;
    annotatorCount?: number;
    ownerId?: string;
}