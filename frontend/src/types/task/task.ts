export enum TaskEventType {
    TASK_CREATED = "TASK_CREATED",
    TASK_ASSIGNED = "TASK_ASSIGNED", 
    TASK_UNASSIGNED = "TASK_UNASSIGNED",
    STAGE_CHANGED = "STAGE_CHANGED",
    STATUS_CHANGED = "STATUS_CHANGED",
    COMMENT_ADDED = "COMMENT_ADDED",
    ANNOTATION_CREATED = "ANNOTATION_CREATED",
    ANNOTATION_UPDATED = "ANNOTATION_UPDATED",
    ANNOTATION_DELETED = "ANNOTATION_DELETED",
    REVIEW_SUBMITTED = "REVIEW_SUBMITTED",
    ISSUE_RAISED = "ISSUE_RAISED",
    PRIORITY_CHANGED = "PRIORITY_CHANGED",
    DUE_DATE_CHANGED = "DUE_DATE_CHANGED",
    TASK_COMPLETED = "TASK_COMPLETED",
    TASK_ARCHIVED = "TASK_ARCHIVED",
    TASK_SUSPENDED = "TASK_SUSPENDED"
}

export enum TaskStatus {
    NOT_STARTED = "NOT_STARTED",
    IN_PROGRESS = "IN_PROGRESS", 
    COMPLETED = "COMPLETED",
    ARCHIVED = "ARCHIVED",
    SUSPENDED = "SUSPENDED"
}

export interface Task {
    id: number;
    priority: number;
    dueDate?: string;
    completedAt?: string;
    archivedAt?: string;
    createdAt: string;
    updatedAt: string;
    assetId: number;
    projectId: number;
    workflowId: number;
    currentWorkflowStageId: number;
    assignedToEmail?: string;
    lastWorkedOnByEmail?: string;
    
    // Extended properties for UI
    status?: TaskStatus;
    assetName?: string;
    workflowName?: string;
    stageName?: string;
    assignedToUserName?: string;
    lastWorkedOnByUserName?: string;
}

export interface TaskEvent {
    id: number;
    eventType: TaskEventType;
    description?: string;
    taskId: number;
    triggeredByUserId?: string;
    triggeredByUserEmail?: string;
    createdAt: string;
    metadata?: string;
}

export interface CreateTaskRequest {
    priority: number;
    dueDate?: string;
    assetId: number;
    workflowId: number;
    currentWorkflowStageId: number;
    assignedToUserId?: string;
}

export interface UpdateTaskRequest {
    priority?: number;
    dueDate?: string;
    currentWorkflowStageId?: number;
    assignedToUserId?: string;
    status?: TaskStatus;
}

export interface TaskWithDetails extends Task {
    events?: TaskEvent[];
    annotationsCount?: number;
    issuesCount?: number;
}

// For table display
export interface TaskTableRow {
    id: number;
    assetId: number; // Add assetId for navigation
    assetName: string;
    priority: number;
    status: TaskStatus;
    assignedTo?: string;
    dueDate?: string;
    completedAt?: string;
    createdAt: string;
    stage: string;
}
