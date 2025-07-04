import type { WorkflowStage } from "./workflowstage";

export interface Workflow {
    id: number;
    name: string;
    description?: string;
    createdAt: string;
    updatedAt: string;
    projectId: number;
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
