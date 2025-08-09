import type { ComputedRef } from 'vue';
import type { TaskTableRow } from './task';

/**
 * Represents the result of a bulk operation on tasks
 */
export interface BulkOperationResult {
    /** Task IDs that were successfully processed */
    succeeded: number[];
    /** Tasks that failed to process with their error messages */
    failed: BulkOperationError[];
    /** Total number of tasks attempted */
    total: number;
    /** Overall success rate (0-1) */
    successRate: number;
}

/**
 * Error information for a failed bulk operation on a specific task
 */
export interface BulkOperationError {
    /** ID of the task that failed */
    taskId: number;
    /** Error message describing what went wrong */
    error: string;
    /** Optional error code for programmatic handling */
    code?: string;
}

/**
 * Options for bulk operations
 */
export interface BulkOperationOptions {
    /** Whether to continue processing if some tasks fail */
    continueOnError?: boolean;
    /** Maximum number of tasks to process in parallel */
    batchSize?: number;
    /** Callback for progress updates */
    onProgress?: (completed: number, total: number) => void;
}

/**
 * Request payload for bulk priority update
 */
export interface BulkPriorityUpdateRequest {
    /** Task IDs to update */
    taskIds: number[];
    /** New priority value (1=Low, 2=Medium, 3+=High) */
    priority: number;
}

/**
 * Request payload for bulk task assignment
 */
export interface BulkAssignmentRequest {
    /** Task IDs to assign */
    taskIds: number[];
    /** Email of the user to assign tasks to, or null to unassign */
    assigneeEmail: string | null;
}

/**
 * Request payload for bulk task archival
 */
export interface BulkArchiveRequest {
    /** Task IDs to archive */
    taskIds: number[];
    /** Optional reason for archiving */
    reason?: string;
}

/**
 * Interface for the task selection manager composable
 */
export interface TaskSelectionManager {
    // Reactive state
    /** Set of currently selected task IDs */
    readonly selectedTaskIds: ComputedRef<Set<number>>;
    /** Number of currently selected tasks */
    readonly selectionCount: ComputedRef<number>;
    /** Whether all tasks on current page are selected (function that takes tasks array) */
    readonly isAllSelectedOnPage: ComputedRef<(tasks: TaskTableRow[]) => boolean>;
    /** Whether some (but not all) tasks on current page are selected (function that takes tasks array) */
    readonly isSomeSelectedOnPage: ComputedRef<(tasks: TaskTableRow[]) => boolean>;
    /** Whether any tasks are selected across all pages */
    readonly hasSelection: ComputedRef<boolean>;

    // Selection actions
    /** Select a specific task */
    selectTask(taskId: number): void;
    /** Deselect a specific task */
    deselectTask(taskId: number): void;
    /** Toggle selection state of a specific task */
    toggleTask(taskId: number): void;
    /** Select all tasks visible on current page */
    selectAllOnPage(tasks: TaskTableRow[]): void;
    /** Deselect all tasks visible on current page */
    deselectAllOnPage(tasks: TaskTableRow[]): void;
    /** Toggle selection of all tasks on current page */
    togglePageSelection(tasks: TaskTableRow[]): void;
    /** Clear all selections across all pages */
    clearSelection(): void;

    // Utility methods
    /** Check if a specific task is selected */
    isTaskSelected(taskId: number): boolean;
    /** Remove selections for task IDs that no longer exist */
    cleanupStaleSelections(validTaskIds: number[]): void;
    /** Get array of selected task IDs (useful for API calls) */
    getSelectedTaskIds(): number[];
}

/**
 * Interface for bulk operations service
 */
export interface TaskBulkOperationsService {
    /** Update priority for multiple tasks */
    bulkUpdatePriority(
        projectId: number,
        request: BulkPriorityUpdateRequest,
        options?: BulkOperationOptions
    ): Promise<BulkOperationResult>;

    /** Assign multiple tasks to a user */
    bulkAssignTasks(
        projectId: number,
        request: BulkAssignmentRequest,
        options?: BulkOperationOptions
    ): Promise<BulkOperationResult>;

    /** Archive multiple tasks */
    bulkArchiveTasks(
        projectId: number,
        request: BulkArchiveRequest,
        options?: BulkOperationOptions
    ): Promise<BulkOperationResult>;
}

/**
 * Selection state for persistence/restoration
 */
export interface SelectionState {
    /** Selected task IDs */
    taskIds: number[];
    /** Timestamp when selection was created */
    timestamp: number;
    /** Optional metadata about the selection context */
    metadata?: {
        projectId?: number;
        stageId?: number;
        totalItems?: number;
    };
}

/**
 * Events emitted by bulk operation components
 */
export interface BulkOperationEvents {
    /** Emitted when a bulk operation starts */
    'operation-start': { operation: string; taskCount: number };
    /** Emitted during bulk operation progress */
    'operation-progress': { completed: number; total: number };
    /** Emitted when a bulk operation completes */
    'operation-complete': { operation: string; result: BulkOperationResult };
    /** Emitted when a bulk operation fails completely */
    'operation-error': { operation: string; error: string };
    /** Emitted when selection changes */
    'selection-change': { count: number; taskIds: number[] };
}