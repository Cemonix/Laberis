import type { WorkflowStage, WorkflowStageType } from "./workflowstage";

export interface Workflow {
    id: number;
    name: string;
    description?: string;
    createdAt: string;
    updatedAt: string;
    projectId: number;
    labelSchemeId: number;
    labelSchemeName?: string;
    stageCount?: number; // Added for UI convenience
}

export interface CreateWorkflowRequest {
    name: string;
    description?: string;
}

export interface UpdateWorkflowRequest {
    name?: string;
    description?: string;
}

export interface WorkflowWithStages extends Workflow {
    stages: WorkflowStage[];
}

export interface CreateWorkflowWithStagesRequest {
    name: string;
    description?: string;
    labelSchemeId: number;
    stages: CreateWorkflowStageWithAssignmentsRequest[];
    includeReviewStage?: boolean;
}

export interface CreateWorkflowStageWithAssignmentsRequest {
    name: string;
    description?: string;
    stageOrder: number;
    stageType?: WorkflowStageType;
    isInitialStage: boolean;
    isFinalStage: boolean;
    inputDataSourceId?: number;
    targetDataSourceId?: number;
    assignedProjectMemberIds: number[];
}
