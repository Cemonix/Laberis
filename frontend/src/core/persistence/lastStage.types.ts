/**
 * Data structure for storing the user's last accessed workflow stage
 */
export interface LastStageData {
    projectId: number;
    workflowId: number;
    stageId: number;
    stageName: string;
    projectName: string;
    lastAccessedAt: number;
}