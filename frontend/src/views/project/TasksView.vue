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
                    v-if="stageType === 'COMPLETION'"
                    variant="secondary"
                    @click="handleExportCoco"
                    :disabled="isLoading"
                    class="export-button"
                >
                    <font-awesome-icon :icon="faDownload" />
                    Export COCO
                </Button>
                <Button 
                    v-if="stageType !== 'ANNOTATION'"
                    variant="secondary" 
                    @click="showVetoedTasks = !showVetoedTasks"
                    class="toggle-vetoed-button"
                >
                    {{ showVetoedTasks ? 'Hide Vetoed' : 'Show Vetoed' }}
                </Button>
                <Button 
                    variant="secondary" 
                    @click="showDeferredTasks = !showDeferredTasks"
                    class="toggle-deferred-button"
                >
                    {{ showDeferredTasks ? 'Hide Deferred' : 'Show Deferred' }}
                </Button>
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
                :can-assign-tasks="canAssignTasks"
                :can-update-task-status="canUpdateTaskStatus"
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
import {computed, onMounted, onUnmounted, ref, watch} from 'vue';
import {useRoute, useRouter} from 'vue-router';
import {FontAwesomeIcon} from '@fortawesome/vue-fontawesome';
import {
    faBolt,
    faDownload,
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
import type { TaskTableRow } from '@/services/project/task/task.types';
import { TaskStatus } from '@/services/project/task/task.types';
import type { TableColumn, TableRowAction } from '@/core/table/table.types';
import { useErrorHandler } from '@/composables/useErrorHandler';
import { usePermissions } from '@/composables/usePermissions';
import { useAssetPreview } from '@/composables/useAssetPreview';
import { PERMISSIONS } from '@/services/auth/permissions.types';
import { useProjectStore } from '@/stores/projectStore';
import { taskService, workflowStageService, assetService, exportService } from '@/services/project';
import { useTaskSelection } from '@/composables/useTaskSelection';
import { taskBulkOperations } from '@/services/project';
import { LastStageManager } from '@/core/persistence';
import { AppLogger } from '@/core/logger/logger';
import { useToast } from '@/composables/useToast';

const route = useRoute();
const router = useRouter();
const { handleError } = useErrorHandler();
const projectStore = useProjectStore();
const { hasProjectPermission } = usePermissions();
const { showToast } = useToast();
const logger = AppLogger.createComponentLogger('TasksView');

// Selection management
const selection = useTaskSelection();

// Asset preview functionality from composable  
const {
    showPreviewPopup, previewAsset, previewPopupStyle, previewImageLoaded,
    showPreview, hidePreview, handlePreviewImageLoad, handlePreviewImageError,
    preloadVisibleAssets, clearAnnotationsCache
} = useAssetPreview();

const projectId = ref<number>(parseInt(route.params.projectId as string));
const workflowId = ref<number>(parseInt(route.params.workflowId as string));
const stageId = ref<number>(parseInt(route.params.stageId as string));
const stageName = ref<string>('');
const stageDescription = ref<string>('');
const stageType = ref<string>('');
const currentStage = ref<any>(null);

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
const showVetoedTasks = ref<boolean>(false);
const showDeferredTasks = ref<boolean>(false);

// Bulk operations state
const isBulkOperationInProgress = ref<boolean>(false);
const bulkOperationProgress = ref<string>('Processing...');

// Computed properties for reactive props
const currentSelectionCount = computed(() => selection.selectionCount.value);

const canManageTasks = computed(() => {
    return hasProjectPermission(PERMISSIONS.PROJECT.UPDATE);
});

const canAssignTasks = computed(() => {
    return hasProjectPermission(PERMISSIONS.TASK.ASSIGN);
});

const canUpdateTaskStatus = computed(() => {
    return hasProjectPermission(PERMISSIONS.TASK.UPDATE_STATUS);
});

const canReviewTasks = computed(() => {
    return hasProjectPermission(PERMISSIONS.ANNOTATION.REVIEW);
});

const isTaskClickable = (task: TaskTableRow): boolean => {
    // Deferred tasks can only be opened by managers
    if (task.status === TaskStatus.DEFERRED && !canManageTasks.value) {
        return false;
    }
    
    // VETOED tasks are view-only and cannot be clicked
    if (task.status === TaskStatus.VETOED) {
        return false;
    }
    
    // Only archived tasks cannot be opened (completed tasks can be viewed in preview mode)
    if (task.status === TaskStatus.ARCHIVED) {
        return false;
    }
    
    return true;
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
                row.status === TaskStatus.ARCHIVED ||
                row.status === TaskStatus.VETOED
        },
        { 
            key: 'priority', 
            label: 'Change Priority', 
            icon: faSort, 
            variant: 'secondary',
            disabled: (row: TaskTableRow) => 
                row.status === TaskStatus.ARCHIVED ||
                row.status === TaskStatus.VETOED
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
                row.status === TaskStatus.ARCHIVED ||
                row.status === TaskStatus.VETOED
        });
    }

    return actions;
});

const taskTableData = computed((): (TaskTableRow & { isClickable: boolean })[] => {
    let filteredTasks = tasks.value;
    
    // Filter vetoed tasks
    if (!showVetoedTasks.value) {
        filteredTasks = filteredTasks.filter(task => task.status !== TaskStatus.VETOED);
    }
    
    // Filter deferred tasks
    if (!showDeferredTasks.value) {
        filteredTasks = filteredTasks.filter(task => task.status !== TaskStatus.DEFERRED);
    }
    
    // Add clickability information to each task
    return filteredTasks.map(task => ({
        ...task,
        isClickable: isTaskClickable(task)
    }));
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
            if (currentStage.value?.inputDataSourceId) {
                const assetsCount = await assetService.getAvailableAssetsCountForDataSource(projectId.value, currentStage.value.inputDataSourceId);
                hasAvailableAssets.value = assetsCount > 0;
                availableAssetsCount.value = assetsCount;
            } else {
                logger.warn('No input data source ID available for stage, assuming no assets available');
                hasAvailableAssets.value = false;
                availableAssetsCount.value = 0;
            }
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

const handleTaskClick = async (task: TaskTableRow, _index: number) => {
    // Early return if task is not clickable (prevents error messages for disabled rows)
    if (!isTaskClickable(task)) {
        logger.debug('Attempted to click non-clickable task', { taskId: task.id, status: task.status });
        return;
    }

    let route = {
        name: 'AnnotationWorkspace',
        params: {
            projectId: projectId.value.toString(),
            assetId: task.assetId.toString()
        },
        query: {
            taskId: task.id.toString(),
            mode: undefined as string | undefined
        }
    };

    // Handle completed tasks in preview mode (no assignment or status change needed)
    if (task.status === TaskStatus.COMPLETED) {
        logger.info('Opening completed task in preview mode', { taskId: task.id, assetId: task.assetId, assetName: task.assetName });

        route.query.mode = 'preview';
        router.push(route);
        return;
    }

    // Only change status to IN_PROGRESS for tasks that are ready for work or suspended
    // DEFERRED tasks can only be changed by managers, so exclude them here
    const shouldChangeToInProgress = [
        TaskStatus.READY_FOR_ANNOTATION,
        TaskStatus.READY_FOR_REVIEW, 
        TaskStatus.READY_FOR_COMPLETION,
        TaskStatus.SUSPENDED
    ].includes(task.status) || (task.status === TaskStatus.DEFERRED && canManageTasks.value);

    try {
        logger.info('Opening task for work', { taskId: task.id, assetName: task.assetName, currentStatus: task.status, willChangeStatus: shouldChangeToInProgress });
        
        // Change status to IN_PROGRESS only if appropriate
        if (shouldChangeToInProgress) {
            await taskService.changeTaskStatus(projectId.value, task.id, {targetStatus: TaskStatus.IN_PROGRESS});
        }
        
        await taskService.assignTaskToCurrentUser(projectId.value, task.id);
        
        logger.info('Navigating to annotation workspace', { taskId: task.id, assetId: task.assetId, assetName: task.assetName });
        router.push(route);
    } catch (error) {
        logger.error('Failed to assign task or navigate', { taskId: task.id, error });
        handleError(error, 'Failed to open task');
    }
};

// Task status management functions
const handleSuspendTask = async (task: TaskTableRow) => {
    try {
        logger.info('Suspending task', { taskId: task.id, assetName: task.assetName });
        
        await taskService.changeTaskStatus(projectId.value, task.id, {
            targetStatus: TaskStatus.SUSPENDED
        });
        
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
        
        await taskService.changeTaskStatus(projectId.value, task.id, {
            targetStatus: TaskStatus.IN_PROGRESS
        });
        
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
        
        await taskService.changeTaskStatus(projectId.value, task.id, {
            targetStatus: TaskStatus.DEFERRED
        });
        
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
        
        await taskService.changeTaskStatus(projectId.value, task.id, {
            targetStatus: TaskStatus.IN_PROGRESS
        });
        
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
        
        await taskService.changeTaskStatus(projectId.value, task.id, {
            targetStatus: TaskStatus.IN_PROGRESS
        });
        
        // Refresh the task list to show updated status
        await loadTasks();
        
        logger.info('Task uncompleted successfully', { taskId: task.id });
    } catch (error) {
        logger.error('Failed to uncomplete task', { taskId: task.id, error });
        handleError(error, 'Failed to mark task as incomplete');
    }
};

const handleReturnTaskForRework = async (task: TaskTableRow, reason?: string) => {
    if (!canReviewTasks.value && !canManageTasks.value) {
        logger.warn('User does not have permission to return tasks for rework');
        return;
    }

    try {
        logger.info('Returning task for rework using pipeline system', { taskId: task.id, assetName: task.assetName, reason });
        
        const result = await taskService.vetoTaskPipeline(projectId.value, task.id, {
            reason: reason || 'Task returned for rework'
        });
        
        if (!result.isSuccess) {
            throw new Error(result.errorMessage || 'Task veto failed');
        }
        
        await loadTasks();
        logger.info('Task returned for rework successfully via pipeline', { taskId: task.id, details: result.details });
    } catch (error) {
        logger.error('Failed to return task for rework via pipeline', { taskId: task.id, error });
        handleError(error, 'Failed to return task for rework');
    }
};

const handleCompleteTask = async (task: TaskTableRow) => {
    try {
        logger.info('Completing task using pipeline system', { taskId: task.id, assetName: task.assetName });
        
        const result = await taskService.completeTaskPipeline(projectId.value, task.id);
        
        if (!result.isSuccess) {
            throw new Error(result.errorMessage || 'Task completion failed');
        }
        
        await loadTasks();
        logger.info('Task completed successfully via pipeline', { taskId: task.id, details: result.details });
    } catch (error) {
        logger.error('Failed to complete task via pipeline', { taskId: task.id, error });
        handleError(error, 'Failed to complete task');
    }
};

const handleArchiveTask = async (task: TaskTableRow) => {
    if (!canManageTasks.value) {
        logger.warn('User does not have permission to archive tasks');
        return;
    }

    try {
        logger.info('Archiving task', { taskId: task.id, assetName: task.assetName });
        
        await taskService.changeTaskStatus(projectId.value, task.id, {
            targetStatus: TaskStatus.ARCHIVED
        });
        
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
            case 'complete':
                await handleCompleteTask(task);
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

// Export functions
const handleExportCoco = async () => {
    if (!currentStage.value || stageType.value !== 'COMPLETION') {
        logger.warn('Export attempted from non-completion stage', { stageType: stageType.value });
        return;
    }

    try {
        logger.info('Starting COCO export', { 
            projectId: projectId.value, 
            workflowStageId: stageId.value,
            stageName: stageName.value 
        });

        showToast('Export', 'Preparing export...', 'info');

        // Use the downloadCocoExport method which handles the download automatically
        await exportService.downloadCocoExport(
            projectId.value,
            stageId.value,
            true, // includeGroundTruth
            false // includePredictions
        );

        showToast('Export', 'Export completed successfully!', 'success');

        logger.info('COCO export completed successfully');
    } catch (error) {
        logger.error('Failed to export COCO data', error);
        handleError(error, 'Failed to export annotations');
        showToast('Export Error', 'Export failed. Please try again.', 'error');
    }
};

// Navigation and utility functions
const handleRefresh = () => {
    // Clear annotations cache to ensure fresh annotation data
    clearAnnotationsCache();
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
        currentStage.value = stage;
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

watch(
    () => route.fullPath,
    (newPath, oldPath) => {
        // Hide preview popup when navigating away
        if (showPreviewPopup.value) {
            hidePreview();
        }
        
        // Clear annotations cache when returning from annotation workspace
        // This ensures fresh annotation data is loaded when previewing assets
        if (oldPath && oldPath.includes('/workspace/') && newPath.includes('/tasks/')) {
            logger.info('Returning from annotation workspace, clearing annotations cache');
            clearAnnotationsCache();
        }
    }
);

onUnmounted(() => {
    if (showPreviewPopup.value) {
        hidePreview();
    }
});

onMounted(async () => {
    // Clear annotations cache on mount to ensure fresh data
    clearAnnotationsCache();
    
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

/* Override cursor for non-clickable task rows */
.tasks-container :deep(.data-table .table tbody tr.clickable[data-clickable="false"]) {
    cursor: not-allowed;
    
    &:hover {
        background: var(--color-gray-50);
        /* Remove the primary-light hover effect */
    }
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