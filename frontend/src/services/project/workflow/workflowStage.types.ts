import type { ProjectMember } from "@/services/project/projectMember.types";

export enum WorkflowStageType {
    ANNOTATION = "ANNOTATION",
    REVISION = "REVISION",
    COMPLETION = "COMPLETION"
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
    
    // Pipeline relationships
    incomingConnections: WorkflowStageConnection[];
    outgoingConnections: WorkflowStageConnection[];
    
    // User assignments
    assignments: WorkflowStageAssignment[];
}

export interface WorkflowStageConnection {
    id: number;
    fromStageId: number;
    toStageId: number;
    condition?: string;
    createdAt: string;
    updatedAt: string;
}

export interface WorkflowStageAssignment {
    id: number;
    workflowStageId: number;
    projectMember: ProjectMember;
    createdAt: string;
    updatedAt: string;
}

export interface WorkflowStagePipeline {
    id: number;
    name: string;
    description?: string;
    stageOrder: number;
    stageType?: WorkflowStageType;
    isInitialStage: boolean;
    isFinalStage: boolean;
    previousStageIds: number[];
    nextStageIds: number[];
    assignedMemberCount: number;
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

export interface CreateWorkflowStageConnectionRequest {
    fromStageId: number;
    toStageId: number;
    condition?: string;
}

export interface UpdateWorkflowStageConnectionRequest {
    fromStageId: number;
    toStageId: number;
    condition?: string;
}

export interface CreateWorkflowStageAssignmentRequest {
    workflowStageId: number;
    projectMemberId: number;
}

export interface WorkflowStageWithUsers extends WorkflowStage {
    assignedUsers: ProjectMember[];
}

// Utility functions for stage types
export const WorkflowStageTypeLabels: Record<WorkflowStageType, string> = {
    [WorkflowStageType.ANNOTATION]: "Annotation",
    [WorkflowStageType.REVISION]: "Revision",
    [WorkflowStageType.COMPLETION]: "Completion"
};

export const formatStageType = (type: WorkflowStageType): string => {
    return WorkflowStageTypeLabels[type] || type;
};