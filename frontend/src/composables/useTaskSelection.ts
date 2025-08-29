import { ref, computed } from 'vue';
import type { TaskSelectionManager, SelectionState } from '@/services/project/task/taskSelection.types';
import type { TaskTableRow } from '@/services/project/task/task.types';
import { AppLogger } from '@/core/logger/logger';

/**
 * Composable for managing task selection state across multiple pages
 * Implements the TaskSelectionManager interface with persistence capabilities
 */
export function useTaskSelection(): TaskSelectionManager & {
    saveSelectionState: () => SelectionState;
    restoreSelectionState: (state: SelectionState) => void;
} {
    const logger = AppLogger.createComponentLogger('TaskSelectionManager');

    // Private reactive state
    const _selectedTaskIds = ref<Set<number>>(new Set());

    // Computed properties implementing the interface
    const selectedTaskIds = computed(() => _selectedTaskIds.value);
    
    const selectionCount = computed(() => _selectedTaskIds.value.size);
    
    const hasSelection = computed(() => _selectedTaskIds.value.size > 0);

    const isAllSelectedOnPage = computed(() => {
        return (tasks: TaskTableRow[]) => {
            if (tasks.length === 0) return false;
            return tasks.every(task => _selectedTaskIds.value.has(task.id));
        };
    });

    const isSomeSelectedOnPage = computed(() => {
        return (tasks: TaskTableRow[]) => {
            if (tasks.length === 0) return false;
            const selectedOnPage = tasks.filter(task => _selectedTaskIds.value.has(task.id)).length;
            return selectedOnPage > 0 && selectedOnPage < tasks.length;
        };
    });

    // Selection actions
    const selectTask = (taskId: number): void => {
        if (_selectedTaskIds.value.has(taskId)) {
            return; // Already selected
        }
        
        const newSelection = new Set(_selectedTaskIds.value);
        newSelection.add(taskId);
        _selectedTaskIds.value = newSelection;
        
        logger.debug('Task selected', { taskId, totalSelected: newSelection.size });
    };

    const deselectTask = (taskId: number): void => {
        if (!_selectedTaskIds.value.has(taskId)) {
            return; // Already deselected
        }
        
        const newSelection = new Set(_selectedTaskIds.value);
        newSelection.delete(taskId);
        _selectedTaskIds.value = newSelection;
        
        logger.debug('Task deselected', { taskId, totalSelected: newSelection.size });
    };

    const toggleTask = (taskId: number): void => {
        if (_selectedTaskIds.value.has(taskId)) {
            deselectTask(taskId);
        } else {
            selectTask(taskId);
        }
    };

    const selectAllOnPage = (tasks: TaskTableRow[]): void => {
        if (tasks.length === 0) {
            logger.warn('Attempted to select all tasks on empty page');
            return;
        }

        const newSelection = new Set(_selectedTaskIds.value);
        let addedCount = 0;

        for (const task of tasks) {
            if (!newSelection.has(task.id)) {
                newSelection.add(task.id);
                addedCount++;
            }
        }

        _selectedTaskIds.value = newSelection;
        logger.info('Selected all tasks on page', { 
            pageSize: tasks.length, 
            addedCount, 
            totalSelected: newSelection.size 
        });
    };

    const deselectAllOnPage = (tasks: TaskTableRow[]): void => {
        if (tasks.length === 0) {
            return;
        }

        const newSelection = new Set(_selectedTaskIds.value);
        let removedCount = 0;

        for (const task of tasks) {
            if (newSelection.has(task.id)) {
                newSelection.delete(task.id);
                removedCount++;
            }
        }

        _selectedTaskIds.value = newSelection;
        logger.info('Deselected all tasks on page', { 
            pageSize: tasks.length, 
            removedCount, 
            totalSelected: newSelection.size 
        });
    };

    const togglePageSelection = (tasks: TaskTableRow[]): void => {
        if (tasks.length === 0) {
            return;
        }

        // If all tasks on page are selected, deselect them
        // Otherwise, select all tasks on page
        const allSelected = tasks.every(task => _selectedTaskIds.value.has(task.id));
        
        if (allSelected) {
            deselectAllOnPage(tasks);
        } else {
            selectAllOnPage(tasks);
        }
    };

    const clearSelection = (): void => {
        const previousCount = _selectedTaskIds.value.size;
        _selectedTaskIds.value = new Set();
        
        logger.info('Selection cleared', { previousCount });
    };

    // Utility methods
    const isTaskSelected = (taskId: number): boolean => {
        return _selectedTaskIds.value.has(taskId);
    };

    const cleanupStaleSelections = (validTaskIds: number[]): void => {
        const validSet = new Set(validTaskIds);
        const newSelection = new Set<number>();
        let removedCount = 0;

        for (const taskId of _selectedTaskIds.value) {
            if (validSet.has(taskId)) {
                newSelection.add(taskId);
            } else {
                removedCount++;
            }
        }

        if (removedCount > 0) {
            _selectedTaskIds.value = newSelection;
            logger.info('Cleaned up stale selections', { 
                removedCount, 
                remainingCount: newSelection.size 
            });
        }
    };

    const getSelectedTaskIds = (): number[] => {
        return Array.from(_selectedTaskIds.value);
    };

    // Persistence methods (optional - can be used for session storage)
    const saveSelectionState = (): SelectionState => {
        return {
            taskIds: getSelectedTaskIds(),
            timestamp: Date.now(),
            metadata: {
                totalItems: _selectedTaskIds.value.size
            }
        };
    };

    const restoreSelectionState = (state: SelectionState): void => {
        if (!state.taskIds || !Array.isArray(state.taskIds)) {
            logger.warn('Invalid selection state provided for restoration');
            return;
        }

        _selectedTaskIds.value = new Set(state.taskIds);
        logger.info('Selection state restored', { 
            count: state.taskIds.length,
            timestamp: state.timestamp 
        });
    };

    // Return the interface implementation
    const manager: TaskSelectionManager = {
        // Reactive state
        selectedTaskIds,
        selectionCount,
        isAllSelectedOnPage,
        isSomeSelectedOnPage,
        hasSelection,

        // Actions
        selectTask,
        deselectTask,
        toggleTask,
        selectAllOnPage,
        deselectAllOnPage,
        togglePageSelection,
        clearSelection,

        // Utilities
        isTaskSelected,
        cleanupStaleSelections,
        getSelectedTaskIds
    };

    // Return with additional persistence methods
    return {
        ...manager,
        saveSelectionState,
        restoreSelectionState
    };
}