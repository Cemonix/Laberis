export interface LastProjectData {
    projectId: number;
    projectName: string;
    lastAccessedAt: number;
}

export interface LastStageData {
    projectId: number;
    workflowId: number;
    stageId: number;
    stageName: string;
    projectName: string;
    lastAccessedAt: number;
}