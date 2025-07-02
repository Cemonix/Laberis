import type { ProjectMember } from '../projectMember/projectMember';

// Export pipeline-specific types
export type {
    Connection,
    PipelineLayout,
    StagePosition,
} from './pipeline';

export interface Workflow {
    id: number;
    name: string;
    description?: string;
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
    assignedUserCount: number;
    positionX?: number;
    positionY?: number;
}

export interface WorkflowUser {
    id: string;
    userName: string;
    email: string;
}

export enum WorkflowStageType {
    ANNOTATION = "ANNOTATION",
    SUSPENDED = "SUSPENDED",
    DEFERRED = "DEFERRED",
    REVIEW = "REVIEW",
    REQUIRES_CHANGES = "REQUIRES_CHANGES",
    ACCEPTED = "ACCEPTED"
}

export interface CreateWorkflowRequest {
    name: string;
    description?: string;
}

export interface UpdateWorkflowRequest {
    name?: string;
    description?: string;
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

export interface CreateWorkflowStageAssignmentRequest {
    workflowStageId: number;
    projectMemberId: number;
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
    [WorkflowStageType.ANNOTATION]: "Annotation",
    [WorkflowStageType.SUSPENDED]: "Suspended",
    [WorkflowStageType.DEFERRED]: "Deferred",
    [WorkflowStageType.REVIEW]: "Review",
    [WorkflowStageType.REQUIRES_CHANGES]: "Requires Changes",
    [WorkflowStageType.ACCEPTED]: "Accepted"
};

export const formatStageType = (type: WorkflowStageType): string => {
    return WorkflowStageTypeLabels[type] || type;
};
