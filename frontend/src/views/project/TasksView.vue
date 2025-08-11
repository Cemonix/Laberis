<template>
    <div class="stage-tasks-view">
        <div class="view-header">
            <div class="header-left">
                <Button 
                    variant="secondary" 
                    @click="goBack"
                    class="back-button"
                >
                    ← Back to Pipeline
                </Button>
                <div class="stage-info">
                    <h2>{{ stageName }} Tasks</h2>
                    <p class="stage-description">{{ stageDescription || 'Manage tasks for this workflow stage' }}</p>
                </div>
            </div>
            <div class="view-actions">
                <Button 
                    variant="primary" 
                    @click="handleRefresh"
                    :disabled="isLoading"
                >
                    <font-awesome-icon :icon="faRefresh" />
                    Refresh
                </Button>
            </div>
        </div>

        <div class="tasks-container">            
            <!-- Bulk Operations Toolbar -->
            <TaskBulkOperationsToolbar
                :selection-count="currentSelectionCount"
                :team-members="projectStore.teamMembers || []"
                :is-operation-in-progress="isBulkOperationInProgress"
                :operation-progress-text="bulkOperationProgress"
                @clear-selection="selection.clearSelection"
                @bulk-priority-change="handleBulkPriorityChange"
                @bulk-assignment="handleBulkAssignment"
            />

            <DataTable
                :data="taskTableData"
                :columns="tableColumns"
                :row-actions="rowActions"
                :is-loading="isLoading"
                :error="errorMessage"
                :clickable-rows="true"
                :pagination="paginationData"
                title="Tasks"
                :empty-message="getEmptyMessage()"
                :empty-description="getEmptyDescription()"
                @action="handleTableAction"
                @row-action="handleRowAction"
                @row-click="handleTaskClick"
                @refresh="handleRefresh"
                @page-change="handlePageChange"
                @sort="handleSort"
            >
                <!-- Custom cell renderers -->

                <template #header-select>
                    <TaskSelectionColumn
                        :show-header="true"
                        :tasks="tasks"
                        :is-task-selected="selection.isTaskSelected"
                        @toggle-page="handleTogglePage"
                    />
                </template>
                
                <template #cell-select="{ row }">
                    <TaskSelectionColumn
                        :task="row"
                        :is-task-selected="selection.isTaskSelected"
                        @toggle-task="handleToggleTask"
                    />
                </template>

                <template #cell-assetName="{ row }">
                    <AssetThumbnailCell
                        :task="row"
                        :project-id="projectId"
                        :all-tasks="tasks"
                        @preview-show="showPreview"
                        @preview-hide="hidePreview"
                    />
                </template>

                <template #cell-priority="{ value }">
                    <TaskPriorityCell :priority="value" />
                </template>

                <template #cell-status="{ value }">
                    <TaskStatusBadge :status="value" />
                </template>
                
                <template #cell-assignedTo="{ value }">
                    <TaskAssigneeCell :assigned-to="value" />
                </template>
                
                <template #cell-dueDate="{ value }">
                    <TaskDueDateCell :due-date="value" />
                </template>
            </DataTable>
        </div>
        
        <!-- Task Status Modal -->
        <TaskStatusModal
            :show="showStatusModal"
            :task="selectedTask"
            :can-review="canReviewTasks"
            :can-manage="canManageTasks"
            :stage-type="stageType"
            @close="handleCloseStatusModal"
            @action="handleStatusAction"
        />
        
        <!-- Edit Task Modal -->
        <EditTaskModal
            :show="showEditModal"
            :task="selectedTask"
            :project-id="projectId"
            :can-assign-tasks="canManageTasks"
            @close="handleCloseEditModal"
            @saved="handleTaskSaved"
        />

        <!-- Task Assignment Modal -->
        <TaskAssignModal
            :show="showAssignModal"
            :task="selectedTask"
            :project-id="projectId"
            @close="handleCloseAssignModal"
            @assigned="handleTaskAssigned"
        />

        <!-- Task Priority Modal -->
        <TaskPriorityModal
            :show="showPriorityModal"
            :task="selectedTask"
            :project-id="projectId"
            @close="handleClosePriorityModal"
            @changed="handlePriorityChanged"
        />

        <!-- Asset Preview Popup -->
        <AssetPreviewPopup
            :visible="showPreviewPopup"
            :preview-asset="previewAsset"
            :popup-style="previewPopupStyle"
            :preview-image-loaded="previewImageLoaded"
            :project-id="projectId"
            @preview-image-load="handlePreviewImageLoad"
            @preview-image-error="handlePreviewImageError"
        />
    </div>
</template>

<script setup lang="ts">
import {computed, onMounted, ref} from 'vue';
import {useRoute, useRouter} from 'vue-router';
import {FontAwesomeIcon} from '@fortawesome/vue-fontawesome';
import {
    faBolt,
    faEdit,
    faRefresh,
    faUserCog,
    faSort
} from '@fortawesome/free-solid-svg-icons';
import Button from '@/components/common/Button.vue';
import DataTable from '@/components/common/DataTable.vue';
import TaskStatusBadge from '@/components/project/task/TaskStatusBadge.vue';
import TaskStatusModal from '@/components/project/task/TaskStatusModal.vue';
import EditTaskModal from '@/components/project/task/EditTaskModal.vue';
import AssetThumbnailCell from '@/components/project/task/AssetThumbnailCell.vue';
import TaskPriorityCell from '@/components/project/task/TaskPriorityCell.vue';
import TaskAssigneeCell from '@/components/project/task/TaskAssigneeCell.vue';
import TaskDueDateCell from '@/components/project/task/TaskDueDateCell.vue';
import AssetPreviewPopup from '@/components/project/task/AssetPreviewPopup.vue';
import TaskAssignModal from '@/components/project/task/TaskAssignModal.vue';
import TaskPriorityModal from '@/components/project/task/TaskPriorityModal.vue';
import TaskSelectionColumn from '@/components/project/task/TaskSelectionColumn.vue';
import TaskBulkOperationsToolbar from '@/components/project/task/TaskBulkOperationsToolbar.vue';
import type {Task, TaskTableRow} from '@/types/task';
import {TaskStatus} from '@/types/task';
import type {TableColumn, TableRowAction} from '@/types/common';
import {useErrorHandler} from '@/composables/useErrorHandler';
import {useProjectPermissions} from '@/composables/useProjectPermissions';
import {useAssetPreview} from '@/composables/useAssetPreview';
import {useProjectStore} from '@/stores/projectStore';
import {taskService, workflowStageService} from '@/services/api/projects';
import {taskStatusService} from '@/services/taskStatusService';
import {useTaskSelection} from '@/composables/useTaskSelection';
import {taskBulkOperations} from '@/services/taskBulkOperations';
import {LastStageManager} from '@/core/storage';
import {AppLogger} from '@/utils/logger';

const route = useRoute();
const router = useRouter();
const { handleError } = useErrorHandler();
const projectStore = useProjectStore();
const { canManageProject } = useProjectPermissions();
const logger = AppLogger.createComponentLogger('TasksView');

// Selection management
const selection = useTaskSelection();

// Asset preview functionality from composable  
const {
    showPreviewPopup, previewAsset, previewPopupStyle, previewImageLoaded,
    showPreview, hidePreview, handlePreviewImageLoad, handlePreviewImageError,
    preloadVisibleAssets
} = useAssetPreview();

const projectId = ref<number>(parseInt(route.params.projectId as string));
const workflowId = ref<number>(parseInt(route.params.workflowId as string));
const stageId = ref<number>(parseInt(route.params.stageId as string));
const stageName = ref<string>('');
const stageDescription = ref<string>('');
const stageType = ref<string>('');

const tasks = ref<TaskTableRow[]>([]);
const isLoading = ref<boolean>(true);
const errorMessage = ref<string | null>(null);
const currentPage = ref<number>(1);
const pageSize = ref<number>(25);
const totalItems = ref<number>(0);
const sortKey = ref<string>('createdAt');
const sortDirection = ref<'asc' | 'desc'>('desc');
const hasAvailableAssets = ref<boolean>(true);
const availableAssetsCount = ref<number>(0);
const showStatusModal = ref<boolean>(false);
const showEditModal = ref<boolean>(false);
const showAssignModal = ref<boolean>(false);
const showPriorityModal = ref<boolean>(false);
const selectedTask = ref<TaskTableRow | null>(null);

// Bulk operations state
const isBulkOperationInProgress = ref<boolean>(false);
const bulkOperationProgress = ref<string>('Processing...');

// Computed properties for reactive props
const currentSelectionCount = computed(() => selection.selectionCount.value);

const canManageTasks = computed(() => {
    return canManageProject.value;
});

const canReviewTasks = computed(() => {
    // For now, assume that managers can also review
    // TODO: reviewer or manager can review task
    return canManageProject.value;
});

const isTaskClickable = (task: TaskTableRow): boolean => {
    // Deferred tasks can only be opened by managers
    if (task.status === TaskStatus.DEFERRED && !canManageTasks.value) {
        return false;
    }
    
    // Only archived tasks cannot be opened (completed tasks can be viewed in preview mode)
    if (task.status === TaskStatus.ARCHIVED) {
        return false;
    }
    
    return true;
};

// Helper to convert TaskTableRow to minimal Task for status service
const createTaskFromRow = (taskRow: TaskTableRow): Task => {
    return {
        id: taskRow.id,
        priority: taskRow.priority,
        dueDate: taskRow.dueDate,
        completedAt: taskRow.completedAt,
        archivedAt: undefined, // Not available in TaskTableRow
        suspendedAt: undefined, // Not available in TaskTableRow
        deferredAt: undefined, // Not available in TaskTableRow
        createdAt: taskRow.createdAt,
        updatedAt: taskRow.createdAt, // Fallback to createdAt
        assetId: taskRow.assetId,
        projectId: projectId.value,
        workflowId: workflowId.value,
        currentWorkflowStageId: stageId.value,
        assignedToEmail: taskRow.assignedTo,
        lastWorkedOnByEmail: undefined, // Not available in TaskTableRow
        status: taskRow.status
    };
};

// Table configuration
const tableColumns: TableColumn[] = [
    { key: 'select', label: '☐', sortable: false, width: '4%', align: 'center' },
    { key: 'assetName', label: 'Asset', sortable: true, width: '26%' },
    { key: 'priority', label: 'Priority', sortable: true, width: '8%', align: 'center' },
    { key: 'status', label: 'Status', sortable: true, width: '12%', align: 'center' },
    { key: 'assignedTo', label: 'Assigned To', sortable: true, width: '16%' },
    { key: 'dueDate', label: 'Due Date', sortable: true, width: '12%', format: 'date' },
    { key: 'createdAt', label: 'Created', sortable: true, width: '11%', format: 'datetime' },
    { key: 'completedAt', label: 'Completed', sortable: true, width: '11%', format: 'datetime' },
];

const rowActions = computed((): TableRowAction<TaskTableRow>[] => {
    const actions: TableRowAction<TaskTableRow>[] = [
        { 
            key: 'edit', 
            label: 'Edit', 
            icon: faEdit, 
            variant: 'secondary'
        },
        { 
            key: 'assign', 
            label: 'Assign', 
            icon: faUserCog, 
            variant: 'secondary',
            disabled: (row: TaskTableRow) => 
                row.status === TaskStatus.SUSPENDED ||
                row.status === TaskStatus.DEFERRED ||
                row.status === TaskStatus.COMPLETED ||
                row.status === TaskStatus.ARCHIVED
        },
        { 
            key: 'priority', 
            label: 'Change Priority', 
            icon: faSort, 
            variant: 'secondary',
            disabled: (row: TaskTableRow) => 
                row.status === TaskStatus.ARCHIVED
        }
    ];

    // Add single status change button for managers only
    if (canManageTasks.value) {
        actions.push({
            key: 'change-status',
            label: 'Change Status',
            icon: faBolt,
            variant: 'secondary',
            disabled: (row: TaskTableRow) => 
                row.status === TaskStatus.COMPLETED ||
                row.status === TaskStatus.ARCHIVED
        });
    }

    return actions;
});

const taskTableData = computed((): TaskTableRow[] => {
    return tasks.value;
});

const paginationData = computed(() => ({
    currentPage: currentPage.value,
    totalPages: Math.ceil(totalItems.value / pageSize.value),
    pageSize: pageSize.value,
    totalItems: totalItems.value,
}));

const loadTasks = async () => {
    if (!projectId.value || !workflowId.value || !stageId.value) {
        errorMessage.value = 'Invalid parameters';
        isLoading.value = false;
        return;
    }

    isLoading.value = true;
    errorMessage.value = null;
    
    try {
        logger.info(`Loading tasks for stage ${stageId.value} in workflow ${workflowId.value}`);
        
        // First check if there are any available assets for task creation (non-blocking)
        try {
            const assetsCheck = await taskService.checkAssetsAvailable(projectId.value);
            hasAvailableAssets.value = assetsCheck.hasAssets;
            availableAssetsCount.value = assetsCheck.count;
        } catch (assetCheckError) {
            logger.warn('Failed to check assets availability, assuming assets are unavailable', assetCheckError);
            hasAvailableAssets.value = false;
            availableAssetsCount.value = 0;
        }
        
        const params = {
            sortBy: sortKey.value,
            isAscending: sortDirection.value === 'asc',
            pageNumber: currentPage.value,
            pageSize: pageSize.value
        };

        const response = await taskService.getEnrichedTasksForStage(
            projectId.value,
            workflowId.value,
            stageId.value,
            params
        );
        
        tasks.value = response.tasks;
        totalItems.value = response.totalCount;
        stageName.value = response.stageName;
        stageDescription.value = response.stageDescription || 'Manage tasks for this workflow stage';
        
        // Clean up selections for tasks that no longer exist
        const currentTaskIds = tasks.value.map(task => task.id);
        selection.cleanupStaleSelections(currentTaskIds);
        
        // Preload assets for visible tasks
        preloadVisibleAssets(projectId.value, tasks.value);
        
    } catch (error) {
        logger.error('Error loading tasks', error);
        errorMessage.value = 'Failed to load tasks';
        handleError(error, 'Loading tasks');
        
        // Reset tasks to empty state on error
        tasks.value = [];
        totalItems.value = 0;
    } finally {
        isLoading.value = false;
    }
};

// Mock functions removed - using real API data only

const handleTableAction = (actionKey: string) => {
    logger.debug('Table action triggered', { actionKey });
    switch (actionKey) {
        case 'import-assets':
            // Navigate to data sources page for asset import
            router.push({
                name: 'ProjectDataSources',
                params: {
                    projectId: projectId.value
                }
            });
            break;
    }
};

const handleRowAction = async (actionKey: string, row: TaskTableRow, index: number) => {
    logger.debug('Row action triggered', { actionKey, taskId: row.id, index });
    switch (actionKey) {
        case 'edit':
            selectedTask.value = row;
            showEditModal.value = true;
            logger.info('Edit task requested', { taskId: row.id });
            break;
        case 'assign':
            selectedTask.value = row;
            showAssignModal.value = true;
            logger.info('Assign task requested', { taskId: row.id });
            break;
        case 'priority':
            selectedTask.value = row;
            showPriorityModal.value = true;
            logger.info('Change priority requested', { taskId: row.id });
            break;
        case 'change-status':
            selectedTask.value = row;
            showStatusModal.value = true;
            break;
    }
};

const handleTogglePage = (tasks: TaskTableRow[]) => {
    selection.togglePageSelection(tasks);
};

const handleToggleTask = (taskId: number) => {
    selection.toggleTask(taskId);
};

const handleTaskClick = (task: TaskTableRow, _index: number) => {
    // Early return if task is not clickable (prevents error messages for disabled rows)
    if (!isTaskClickable(task)) {
        logger.debug('Attempted to click non-clickable task', { taskId: task.id, status: task.status });
        return;
    }
    
    // Handle completed tasks in preview mode
    if (task.status === TaskStatus.COMPLETED) {
        logger.info('Opening completed task in preview mode', { taskId: task.id, assetId: task.assetId, assetName: task.assetName });
        
        // For completed tasks, navigate to workspace with preview mode
        // The asset has moved to the next data source, so we track by asset ID
        router.push({
            name: 'AnnotationWorkspace',
            params: {
                projectId: projectId.value.toString(),
                assetId: task.assetId.toString()
            },
            query: {
                mode: 'preview',
                taskId: task.id.toString()
            }
        });
        return;
    }
    
    // Handle regular (editable) tasks
    logger.info('Navigating to annotation workspace', { taskId: task.id, assetId: task.assetId, assetName: task.assetName });
    
    // Navigate to annotation workspace using projectId and the task's assetId
    // The workspace will load the asset and any existing annotations
    router.push({
        name: 'AnnotationWorkspace',
        params: {
            projectId: projectId.value.toString(),
            assetId: task.assetId.toString()
        },
        query: {
            taskId: task.id.toString() // Pass taskId as query parameter for context
        }
    });
};

// Task status management functions
const handleSuspendTask = async (task: TaskTableRow) => {
    try {
        logger.info('Suspending task', { taskId: task.id, assetName: task.assetName });
        
        const taskData = createTaskFromRow(task);
        await taskStatusService.suspendTask(projectId.value, taskData, canManageTasks.value);
        
        // Refresh the task list to show updated status
        await loadTasks();
        
        logger.info('Task suspended successfully', { taskId: task.id });
    } catch (error) {
        logger.error('Failed to suspend task', { taskId: task.id, error });
        handleError(error, 'Failed to suspend task');
    }
};

const handleUnsuspendTask = async (task: TaskTableRow) => {
    try {
        logger.info('Unsuspending task', { taskId: task.id, assetName: task.assetName });
        
        const taskData = createTaskFromRow(task);
        await taskStatusService.resumeTask(projectId.value, taskData, canManageTasks.value);
        
        // Refresh the task list to show updated status
        await loadTasks();
        
        logger.info('Task unsuspended successfully', { taskId: task.id });
    } catch (error) {
        logger.error('Failed to unsuspend task', { taskId: task.id, error });
        handleError(error, 'Failed to unsuspend task');
    }
};

const handleDeferTask = async (task: TaskTableRow) => {
    try {
        logger.info('Deferring task', { taskId: task.id, assetName: task.assetName });
        
        const taskData = createTaskFromRow(task);
        await taskStatusService.deferTask(projectId.value, taskData, canManageTasks.value);
        
        // Refresh the task list to show updated status
        await loadTasks();
        
        logger.info('Task deferred successfully', { taskId: task.id });
    } catch (error) {
        logger.error('Failed to defer task', { taskId: task.id, error });
        handleError(error, 'Failed to defer task');
    }
};

const handleUndeferTask = async (task: TaskTableRow) => {
    try {
        logger.info('Undeferring task', { taskId: task.id, assetName: task.assetName });
        
        const taskData = createTaskFromRow(task);
        await taskStatusService.undeferTask(projectId.value, taskData, canManageTasks.value);
        
        // Refresh the task list to show updated status
        await loadTasks();
        
        logger.info('Task undeferred successfully', { taskId: task.id });
    } catch (error) {
        logger.error('Failed to undefer task', { taskId: task.id, error });
        handleError(error, 'Failed to undefer task');
    }
};

const handleUncompleteTask = async (task: TaskTableRow) => {
    try {
        logger.info('Uncompleting task', { taskId: task.id, assetName: task.assetName });
        
        const taskData = createTaskFromRow(task);
        await taskStatusService.uncompleteTask(projectId.value, taskData, canManageTasks.value);
        
        // Refresh the task list to show updated status
        await loadTasks();
        
        logger.info('Task uncompleted successfully', { taskId: task.id });
    } catch (error) {
        logger.error('Failed to uncomplete task', { taskId: task.id, error });
        handleError(error, 'Failed to mark task as incomplete');
    }
};

const handleReturnTaskForRework = async (task: TaskTableRow, reason?: string) => {
    // Only reviewers and managers can return tasks for rework
    if (!canReviewTasks.value && !canManageTasks.value) {
        logger.warn('User does not have permission to return tasks for rework');
        return;
    }

    try {
        logger.info('Returning task for rework', { taskId: task.id, assetName: task.assetName, reason });
        
        // Convert TaskTableRow to Task object for the service
        const taskData: Task = createTaskFromRow(task);
        
        await taskStatusService.returnTaskForRework(projectId.value, taskData, reason);
        
        // Refresh the task list to show updated status
        await loadTasks();
        
        logger.info('Task returned for rework successfully', { taskId: task.id });
    } catch (error) {
        logger.error('Failed to return task for rework', { taskId: task.id, error });
        handleError(error, 'Failed to return task for rework');
    }
};

const handleArchiveTask = async (task: TaskTableRow) => {
    if (!canManageTasks.value) {
        logger.warn('User does not have permission to archive tasks');
        return;
    }

    try {
        logger.info('Archiving task', { taskId: task.id, assetName: task.assetName });
        
        const taskData = createTaskFromRow(task);
        await taskStatusService.archiveTask(projectId.value, taskData, canManageTasks.value);
        
        // Refresh the task list to show updated status
        await loadTasks();
        
        logger.info('Task archived successfully', { taskId: task.id });
    } catch (error) {
        logger.error('Failed to archive task', { taskId: task.id, error });
        handleError(error, 'Failed to archive task');
    }
};

// Modal handlers
const handleCloseStatusModal = () => {
    showStatusModal.value = false;
    selectedTask.value = null;
};

const handleCloseEditModal = () => {
    showEditModal.value = false;
    selectedTask.value = null;
};

const handleCloseAssignModal = () => {
    showAssignModal.value = false;
    selectedTask.value = null;
};

const handleClosePriorityModal = () => {
    showPriorityModal.value = false;
    selectedTask.value = null;
};

const handleTaskSaved = async (updatedTask: TaskTableRow) => {
    // Update the task in the local tasks array
    const taskIndex = tasks.value.findIndex(task => task.id === updatedTask.id);
    if (taskIndex !== -1) {
        tasks.value[taskIndex] = updatedTask;
    }
    
    logger.info('Task updated in local state', { taskId: updatedTask.id });
    
    // Close the modal
    handleCloseEditModal();
};

const handleTaskAssigned = async (updatedTask: TaskTableRow) => {
    // Update the task in the local tasks array
    const taskIndex = tasks.value.findIndex(task => task.id === updatedTask.id);
    if (taskIndex !== -1) {
        tasks.value[taskIndex] = updatedTask;
    }
    
    logger.info('Task assignment updated in local state', { taskId: updatedTask.id, assignedTo: updatedTask.assignedTo });
    
    // Close the modal
    handleCloseAssignModal();
};

const handlePriorityChanged = async (updatedTask: TaskTableRow) => {
    // Update the task in the local tasks array
    const taskIndex = tasks.value.findIndex(task => task.id === updatedTask.id);
    if (taskIndex !== -1) {
        tasks.value[taskIndex] = updatedTask;
    }
    
    logger.info('Task priority updated in local state', { taskId: updatedTask.id, priority: updatedTask.priority });
    
    // Close the modal
    handleClosePriorityModal();
};

// Bulk operation handlers
const handleBulkPriorityChange = async (priority: number) => {
    const selectedIds = selection.getSelectedTaskIds();
    if (selectedIds.length === 0) return;

    isBulkOperationInProgress.value = true;
    bulkOperationProgress.value = `Updating priority for ${selectedIds.length} tasks...`;

    try {
        const result = await taskBulkOperations.bulkUpdatePriority(
            projectId.value,
            { taskIds: selectedIds, priority },
            {
                onProgress: (completed, total) => {
                    bulkOperationProgress.value = `Updated ${completed}/${total} tasks...`;
                }
            }
        );

        logger.info('Bulk priority change completed', result);

        // Show results and refresh data
        if (result.succeeded.length > 0) {
            // Clear selection and refresh tasks
            selection.clearSelection();
            await loadTasks();
        }

        if (result.failed.length > 0) {
            handleError(
                new Error(`Failed to update ${result.failed.length} tasks`),
                'Some tasks could not be updated'
            );
        }
    } catch (error) {
        logger.error('Bulk priority change failed', error);
        handleError(error, 'Failed to update task priorities');
    } finally {
        isBulkOperationInProgress.value = false;
    }
};

const handleBulkAssignment = async (assigneeEmail: string | null) => {
    const selectedIds = selection.getSelectedTaskIds();
    if (selectedIds.length === 0) return;

    isBulkOperationInProgress.value = true;
    const action = assigneeEmail ? `Assigning ${selectedIds.length} tasks to ${assigneeEmail}...` : `Unassigning ${selectedIds.length} tasks...`;
    bulkOperationProgress.value = action;

    try {
        const result = await taskBulkOperations.bulkAssignTasks(
            projectId.value,
            { taskIds: selectedIds, assigneeEmail },
            {
                onProgress: (completed, total) => {
                    const verb = assigneeEmail ? 'Assigned' : 'Unassigned';
                    bulkOperationProgress.value = `${verb} ${completed}/${total} tasks...`;
                }
            }
        );

        logger.info('Bulk assignment completed', result);

        if (result.succeeded.length > 0) {
            selection.clearSelection();
            await loadTasks();
        }

        if (result.failed.length > 0) {
            handleError(
                new Error(`Failed to assign ${result.failed.length} tasks`),
                'Some tasks could not be assigned'
            );
        }
    } catch (error) {
        logger.error('Bulk assignment failed', error);
        handleError(error, 'Failed to assign tasks');
    } finally {
        isBulkOperationInProgress.value = false;
    }
};


const handleStatusAction = async (actionKey: string, task: TaskTableRow) => {
    logger.debug('Status action triggered', { actionKey, taskId: task.id });
    
    try {
        switch (actionKey) {
            case 'suspend':
                await handleSuspendTask(task);
                break;
            case 'unsuspend':
                await handleUnsuspendTask(task);
                break;
            case 'defer':
                await handleDeferTask(task);
                break;
            case 'undefer':
                await handleUndeferTask(task);
                break;
            case 'uncomplete':
                await handleUncompleteTask(task);
                break;
            case 'return_for_rework':
                await handleReturnTaskForRework(task);
                break;
            case 'archive':
                await handleArchiveTask(task);
                break;
            default:
                logger.warn('Unknown status action', { actionKey });
                return;
        }
        
        // Close modal after successful action
        handleCloseStatusModal();
    } catch (error) {
        // Error handling is done in individual functions
        // Modal stays open to allow retry
        logger.debug('Status action failed, keeping modal open for retry');
    }
};

// Navigation and utility functions
const handleRefresh = () => {
    loadTasks();
};

const handlePageChange = (page: number) => {
    currentPage.value = page;
    loadTasks();
};

const handleSort = (key: string, direction: 'asc' | 'desc') => {
    sortKey.value = key;
    sortDirection.value = direction;
    loadTasks();
};

const goBack = () => {
    router.push({
        name: 'WorkflowPipeline',
        params: {
            projectId: projectId.value,
            workflowId: workflowId.value
        }
    });
};

// Helper functions moved to respective cell components

const getEmptyMessage = (): string => {
    if (!hasAvailableAssets.value) {
        return 'No assets available for task creation';
    }
    return 'No tasks found';
};

const getEmptyDescription = (): string => {
    if (!hasAvailableAssets.value) {
        return 'There are no assets imported in this project yet. Assets need to be imported into data sources before tasks can be automatically created. Go to the Data Sources page to import assets.';
    }
    return 'Tasks will be automatically created when assets are added to this workflow stage.';
};

const loadStageInfo = async () => {
    try {
        const stage = await workflowStageService.getWorkflowStageById(
            projectId.value, 
            workflowId.value, 
            stageId.value
        );
        stageName.value = stage.name;
        stageDescription.value = stage.description || '';
        stageType.value = stage.stageType || '';
        
        // Update the project store with current stage type for reactive navbar
        projectStore.setCurrentStageType(stage.stageType || '');
        
        // Save this stage as the user's last accessed stage
        try {
            const projectName = projectStore.currentProject?.name || `Project ${projectId.value}`;
            LastStageManager.saveLastStage(
                projectId.value,
                workflowId.value,
                stageId.value,
                stage.name || `Stage ${stageId.value}`,
                projectName
            );
            
            logger.info('Saved last accessed stage', {
                projectId: projectId.value,
                workflowId: workflowId.value,
                stageId: stageId.value,
                stageName: stage.name
            });
        } catch (saveError) {
            logger.warn('Failed to save last stage:', saveError);
        }
        
        logger.info('Loaded stage information', { 
            stageId: stageId.value,
            stageName: stageName.value, 
            stageType: stageType.value 
        });
    } catch (error) {
        logger.error('Failed to load stage information', error);
        // Don't fail the entire component if stage info fails to load
    }
};

onMounted(async () => {
    // Load project data first (includes team members for permissions)
    try {
        await projectStore.setCurrentProject(projectId.value);
    } catch (error) {
        logger.warn('Failed to load project data, continuing with tasks', error);
    }
    
    // Load stage information
    await loadStageInfo();
    
    // Then load tasks
    loadTasks();
});
</script>

<style lang="scss" scoped>
.stage-tasks-view {
    display: flex;
    flex-direction: column;
    height: 100%;
    min-height: 0;
}

.view-header {
    flex-shrink: 0;
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    padding: 1.5rem;
    background-color: var(--color-white);
    border-bottom: 1px solid var(--color-gray-400);
    gap: 1.5rem;
}

.header-left {
    display: flex;
    align-items: flex-start;
    gap: 1rem;
    flex-grow: 1;
}

.back-button {
    flex-shrink: 0;
    margin-top: 2px; // Align with title
}

.stage-info {
    flex-grow: 1;
    min-width: 0;
    
    h2 {
        margin: 0 0 0.25rem;
        color: var(--color-gray-800);
        font-size: 1.5rem;
        line-height: 1.2;
    }
    
    .stage-description {
        margin: 0;
        color: var(--color-gray-600);
        font-size: 0.875rem;
        line-height: 1.4;
    }
}

.view-actions {
    flex-shrink: 0;
    display: flex;
    gap: 0.5rem;
}

.tasks-container {
    flex-grow: 1;
    padding: 1.5rem;
    overflow: hidden;
}

@media (max-width: 768px) {
    .view-header {
        flex-direction: column;
        align-items: stretch;
        gap: 1rem;
    }
    
    .header-left {
        flex-direction: column;
        align-items: stretch;
        gap: 0.5rem;
    }
    
    .back-button {
        align-self: flex-start;
        margin-top: 0;
    }
    
    .view-actions {
        justify-content: center;
    }
    
    .tasks-container {
        padding: 1rem;
    }
}
</style>