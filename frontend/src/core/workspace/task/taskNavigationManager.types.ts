/**
 * Result types for navigation operations
 */
export interface NavigationResult {
    success: boolean;
    navigation: {
        projectId: string;
        assetId: string;
        taskId: string;
    } | null;
    error?: string;
}

/**
 * Navigation information for UI state
 */
export interface NavigationInfo {
    currentIndex: number;
    totalTasks: number;
    hasNext: boolean;
    hasPrevious: boolean;
}