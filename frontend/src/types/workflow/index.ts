export interface Workflow {
    id: number;
    name: string;
    description?: string;
    isDefault: boolean;
    createdAt: string;
    updatedAt: string;
    projectId: number;
    stageCount?: number; // Added for UI convenience
}

export interface WorkflowStage {
    id: number;
    name: string;
    description?: string;
    stageOrder: number;
    stageType?: WorkflowStageType;
    isInitialStage: boolean;
    isFinalStage: boolean;
    createdAt: string;
    updatedAt: string;
    workflowId: number;
    inputDataSourceId?: number;
    targetDataSourceId?: number;
    assignedUsers?: WorkflowUser[];
}

export interface WorkflowUser {
    id: string;
    userName: string;
    email: string;
}

export enum WorkflowStageType {
    INITIAL_IMPORT = "INITIAL_IMPORT",
    PREPROCESSING = "PREPROCESSING",
    ANNOTATION = "ANNOTATION",
    REVIEW = "REVIEW",
    QUALITY_ASSURANCE = "QUALITY_ASSURANCE",
    AUTO_LABELING = "AUTO_LABELING",
    EXPORT = "EXPORT",
    FINAL_ACCEPTANCE = "FINAL_ACCEPTANCE"
}

export interface CreateWorkflowRequest {
    name: string;
    description?: string;
    isDefault?: boolean;
}

export interface UpdateWorkflowRequest {
    name?: string;
    description?: string;
    isDefault?: boolean;
}

export interface CreateWorkflowStageRequest {
    name: string;
    description?: string;
    stageOrder: number;
    stageType?: WorkflowStageType;
    isInitialStage?: boolean;
    isFinalStage?: boolean;
    inputDataSourceId?: number;
    targetDataSourceId?: number;
}

export interface UpdateWorkflowStageRequest {
    name?: string;
    description?: string;
    stageOrder?: number;
    stageType?: WorkflowStageType;
    isInitialStage?: boolean;
    isFinalStage?: boolean;
    inputDataSourceId?: number;
    targetDataSourceId?: number;
}

// Helper types for API responses
export interface WorkflowWithStages extends Workflow {
    stages: WorkflowStage[];
}

export interface WorkflowStageWithUsers extends WorkflowStage {
    assignedUsers: WorkflowUser[];
}

// Utility functions for stage types
export const WorkflowStageTypeLabels: Record<WorkflowStageType, string> = {
    [WorkflowStageType.INITIAL_IMPORT]: "Initial Import",
    [WorkflowStageType.PREPROCESSING]: "Preprocessing",
    [WorkflowStageType.ANNOTATION]: "Annotation",
    [WorkflowStageType.REVIEW]: "Review",
    [WorkflowStageType.QUALITY_ASSURANCE]: "Quality Assurance",
    [WorkflowStageType.AUTO_LABELING]: "Auto Labeling",
    [WorkflowStageType.EXPORT]: "Export",
    [WorkflowStageType.FINAL_ACCEPTANCE]: "Final Acceptance"
};

export const formatStageType = (type: WorkflowStageType): string => {
    return WorkflowStageTypeLabels[type] || type;
};
