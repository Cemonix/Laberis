/**
 * Response from task navigation endpoints
 */
export interface TaskNavigationResponse {
  /** ID of the next/previous task, null if none available */
  taskId?: number | null;
  
  /** Asset ID of the next/previous task, null if none available */
  assetId?: number | null;
  
  /** Whether there is a next task available */
  hasNext: boolean;
  
  /** Whether there is a previous task available */
  hasPrevious: boolean;
  
  /** Current position in the navigation sequence (1-based) */
  currentPosition: number;
  
  /** Total number of navigable tasks */
  totalTasks: number;
  
  /** Optional message for user feedback */
  message?: string | null;
  
  /** Number of available tasks (unlocked and can be worked on) */
  availableTasks: number;
  
  /** Number of completed tasks by the current user */
  completedTasks: number;
  
  /** Number of assigned tasks (being worked on by others) */
  assignedTasks: number;
}

/**
 * Information for navigating to a task
 */
export interface TaskNavigationInfo {
  projectId: string;
  assetId: string;
  taskId: string;
}