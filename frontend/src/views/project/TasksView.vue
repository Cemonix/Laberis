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
            <DataTable
                :data="taskTableData"
                :columns="tableColumns"
                :actions="getTableActions()"
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
                <template #cell-assetName="{ value, row }">
                    <div class="asset-preview-cell" @mouseenter="showPreview($event, row)" @mouseleave="hidePreview">
                        <div class="asset-thumbnail">
                            <img 
                                :src="getAssetThumbnailUrl(projectId, row.assetId)" 
                                :alt="value"
                                class="thumbnail-image"
                                @error="(event) => handleImageError(event, tasks)"
                                :class="{ 'loading': loadingAssets.has(row.assetId) }"
                            />
                            <div v-if="loadingAssets.has(row.assetId)" class="loading-overlay">
                                <font-awesome-icon :icon="faRefresh" spin class="loading-icon" />
                            </div>
                            <div v-if="hasImageError(row.assetId)" class="error-overlay">
                                <font-awesome-icon :icon="faExclamationTriangle" class="error-icon" />
                                <span class="error-text">Image Error</span>
                            </div>
                            <!-- Annotation indicators -->
                            <div v-if="getAnnotationCount(row.assetId) > 0" class="annotation-indicator">
                                <font-awesome-icon :icon="faShapes" class="annotation-icon" />
                                <span class="annotation-count">{{ getAnnotationCount(row.assetId) }}</span>
                            </div>
                            <!-- Annotation overlay -->
                            <svg 
                                v-if="getAnnotationCount(row.assetId) > 0" 
                                class="annotation-overlay" 
                                viewBox="0 0 128 128"
                            >
                                <g v-for="annotation in getVisibleAnnotations(row.assetId)" :key="annotation.annotationId">
                                    <!-- Bounding box annotations -->
                                    <rect 
                                        v-if="isBoundingBoxAnnotation(annotation)"
                                        :x="scaleCoordinate(annotation.coordinates.topLeft.x, loadedAssets.get(row.assetId)?.width, 128)"
                                        :y="scaleCoordinate(annotation.coordinates.topLeft.y, loadedAssets.get(row.assetId)?.height, 128)"
                                        :width="scaleCoordinate(annotation.coordinates.bottomRight.x - annotation.coordinates.topLeft.x, loadedAssets.get(row.assetId)?.width, 128)"
                                        :height="scaleCoordinate(annotation.coordinates.bottomRight.y - annotation.coordinates.topLeft.y, loadedAssets.get(row.assetId)?.height, 128)"
                                        fill="none"
                                        stroke="#3b82f6"
                                        stroke-width="1.5"
                                        opacity="0.8"
                                    />
                                    <!-- Point annotations -->
                                    <circle 
                                        v-if="isPointAnnotation(annotation)"
                                        :cx="scaleCoordinate(annotation.coordinates.point.x, loadedAssets.get(row.assetId)?.width, 128)"
                                        :cy="scaleCoordinate(annotation.coordinates.point.y, loadedAssets.get(row.assetId)?.height, 128)"
                                        r="3"
                                        fill="#3b82f6"
                                        opacity="0.8"
                                    />
                                    <!-- Polygon annotations -->
                                    <polygon 
                                        v-if="isPolygonAnnotation(annotation)"
                                        :points="getScaledPolygonPoints(annotation, row.assetId, 128)"
                                        fill="rgba(59, 130, 246, 0.3)"
                                        stroke="#3b82f6"
                                        stroke-width="1.5"
                                        opacity="0.8"
                                    />
                                    <!-- Polyline annotations -->
                                    <polyline 
                                        v-if="isPolylineAnnotation(annotation)"
                                        :points="getScaledPolygonPoints(annotation, row.assetId, 128)"
                                        fill="none"
                                        stroke="#3b82f6"
                                        stroke-width="1.5"
                                        opacity="0.8"
                                    />
                                    <!-- Line annotations -->
                                    <line 
                                        v-if="isLineAnnotation(annotation)"
                                        :x1="scaleCoordinate(annotation.coordinates.pointFrom.x, loadedAssets.get(row.assetId)?.width, 128)"
                                        :y1="scaleCoordinate(annotation.coordinates.pointFrom.y, loadedAssets.get(row.assetId)?.height, 128)"
                                        :x2="scaleCoordinate(annotation.coordinates.pointTo.x, loadedAssets.get(row.assetId)?.width, 128)"
                                        :y2="scaleCoordinate(annotation.coordinates.pointTo.y, loadedAssets.get(row.assetId)?.height, 128)"
                                        stroke="#3b82f6"
                                        stroke-width="1.5"
                                        opacity="0.8"
                                    />
                                </g>
                            </svg>
                        </div>
                    </div>
                </template>

                <template #cell-priority="{ value }">
                    <div class="priority-cell" :class="getPriorityClass(value)">
                        <font-awesome-icon :icon="getPriorityIcon(value)" />
                        {{ getPriorityLabel(value) }}
                    </div>
                </template>

                <template #cell-status="{ value }">
                    <TaskStatusBadge :status="value" />
                </template>
                
                <template #cell-assignedTo="{ value }">
                    <div v-if="value" class="assigned-user">
                        <font-awesome-icon :icon="faUser" class="user-icon" />
                        {{ value }}
                    </div>
                    <div v-else class="unassigned">
                        <font-awesome-icon :icon="faUserSlash" class="unassigned-icon" />
                        Unassigned
                    </div>
                </template>
                
                <template #cell-dueDate="{ value }">
                    <div v-if="value" class="due-date" :class="getDueDateClass(value)">
                        <font-awesome-icon :icon="faCalendar" />
                        {{ formatDate(value) }}
                    </div>
                    <span v-else class="no-due-date">No due date</span>
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

        <!-- Asset Preview Popup -->
        <div 
            v-if="showPreviewPopup" 
            class="asset-preview-popup"
            :style="previewPopupStyle"
        >
            <div class="preview-header">
                <span class="preview-asset-name">{{ previewAsset?.assetName }}</span>
                <div v-if="previewAsset && loadedAssets.get(previewAsset.assetId)" class="asset-metadata">
                    <span class="asset-size">{{ formatFileSize(loadedAssets.get(previewAsset.assetId)?.sizeBytes) }}</span>
                    <span class="asset-dimensions" v-if="loadedAssets.get(previewAsset.assetId)?.width && loadedAssets.get(previewAsset.assetId)?.height">
                        {{ loadedAssets.get(previewAsset.assetId)?.width }}×{{ loadedAssets.get(previewAsset.assetId)?.height }}
                    </span>
                    <span v-if="getAnnotationCount(previewAsset.assetId) > 0" class="annotation-info">
                        <font-awesome-icon :icon="faShapes" />
                        {{ getAnnotationCount(previewAsset.assetId) }} annotation{{ getAnnotationCount(previewAsset.assetId) === 1 ? '' : 's' }}
                    </span>
                </div>
            </div>
            <div class="preview-image-container">
                <div v-if="previewAsset && loadingAssets.has(previewAsset.assetId)" class="loading-state">
                    <font-awesome-icon :icon="faRefresh" spin class="loading-icon-large" />
                    <span>Loading preview...</span>
                </div>
                <div v-else-if="previewAsset && hasImageError(previewAsset.assetId)" class="error-state">
                    <font-awesome-icon :icon="faExclamationTriangle" class="error-icon-large" />
                    <span>Image failed to load</span>
                </div>
                <div v-else class="preview-image-wrapper">
                    <img 
                        :src="getAssetFullUrl(projectId, previewAsset?.assetId)" 
                        :alt="previewAsset?.assetName"
                        class="preview-image"
                        @error="handlePreviewImageError"
                        @load="handlePreviewImageLoad"
                    />
                    <!-- Annotation overlay for preview -->
                    <svg 
                        v-if="previewAsset && getAnnotationCount(previewAsset.assetId) > 0 && previewImageLoaded" 
                        class="preview-annotation-overlay" 
                        :viewBox="`0 0 ${getAssetDimensions(previewAsset.assetId).width} ${getAssetDimensions(previewAsset.assetId).height}`"
                    >
                        <g v-for="annotation in getVisibleAnnotations(previewAsset.assetId)" :key="annotation.annotationId">
                            <!-- Bounding box annotations -->
                            <rect 
                                v-if="isBoundingBoxAnnotation(annotation)"
                                :x="annotation.coordinates.topLeft.x"
                                :y="annotation.coordinates.topLeft.y"
                                :width="annotation.coordinates.bottomRight.x - annotation.coordinates.topLeft.x"
                                :height="annotation.coordinates.bottomRight.y - annotation.coordinates.topLeft.y"
                                fill="none"
                                stroke="#3b82f6"
                                stroke-width="2"
                                opacity="0.8"
                            />
                            <!-- Point annotations -->
                            <circle 
                                v-if="isPointAnnotation(annotation)"
                                :cx="annotation.coordinates.point.x"
                                :cy="annotation.coordinates.point.y"
                                r="8"
                                fill="#3b82f6"
                                opacity="0.8"
                                stroke="white"
                                stroke-width="1"
                            />
                            <!-- Polygon annotations -->
                            <polygon 
                                v-if="isPolygonAnnotation(annotation)"
                                :points="getPolygonPoints(annotation)"
                                fill="rgba(59, 130, 246, 0.3)"
                                stroke="#3b82f6"
                                stroke-width="2"
                                opacity="0.8"
                            />
                            <!-- Polyline annotations -->
                            <polyline 
                                v-if="isPolylineAnnotation(annotation)"
                                :points="getPolygonPoints(annotation)"
                                fill="none"
                                stroke="#3b82f6"
                                stroke-width="2"
                                opacity="0.8"
                            />
                            <!-- Line annotations -->
                            <line 
                                v-if="isLineAnnotation(annotation)"
                                :x1="annotation.coordinates.pointFrom.x"
                                :y1="annotation.coordinates.pointFrom.y"
                                :x2="annotation.coordinates.pointTo.x"
                                :y2="annotation.coordinates.pointTo.y"
                                stroke="#3b82f6"
                                stroke-width="2"
                                opacity="0.8"
                            />
                        </g>
                    </svg>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import {computed, onMounted, ref} from 'vue';
import {useRoute, useRouter} from 'vue-router';
import {FontAwesomeIcon} from '@fortawesome/vue-fontawesome';
import {
    faArrowUp,
    faBolt,
    faCalendar,
    faEdit,
    faExclamationTriangle,
    faMinus,
    faPlay,
    faPlus,
    faRefresh,
    faUser,
    faUserCog,
    faUserSlash,
    faShapes
} from '@fortawesome/free-solid-svg-icons';
import Button from '@/components/common/Button.vue';
import DataTable from '@/components/common/DataTable.vue';
import TaskStatusBadge from '@/components/project/task/TaskStatusBadge.vue';
import TaskStatusModal from '@/components/project/task/TaskStatusModal.vue';
import EditTaskModal from '@/components/project/task/EditTaskModal.vue';
import type {Task, TaskTableRow} from '@/types/task';
import {TaskStatus} from '@/types/task';
import type {TableAction, TableColumn, TableRowAction} from '@/types/common';
import {useErrorHandler} from '@/composables/useErrorHandler';
import {useProjectPermissions} from '@/composables/useProjectPermissions';
import {useAssetPreview} from '@/composables/useAssetPreview';
import {useProjectStore} from '@/stores/projectStore';
import {taskService, workflowStageService} from '@/services/api/projects';
import {taskStatusService} from '@/services/taskStatusService';
import {AppLogger} from '@/utils/logger';

const route = useRoute();
const router = useRouter();
const { handleError } = useErrorHandler();
const projectStore = useProjectStore();
const { canManageProject } = useProjectPermissions();
const logger = AppLogger.createComponentLogger('TasksView');

// Asset preview functionality from composable
const {
    loadedAssets, loadingAssets,
    showPreviewPopup, previewAsset, previewPopupStyle, previewImageLoaded,
    getAssetThumbnailUrl, getAssetFullUrl, getAnnotationCount, getVisibleAnnotations,
    isBoundingBoxAnnotation, isPointAnnotation, isPolygonAnnotation, 
    isPolylineAnnotation, isLineAnnotation, getPolygonPoints, scaleCoordinate,
    getScaledPolygonPoints, getAssetDimensions, hasImageError, showPreview, 
    hidePreview, handleImageError, handlePreviewImageLoad, handlePreviewImageError,
    preloadVisibleAssets, formatFileSize
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
const selectedTask = ref<TaskTableRow | null>(null);

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
    { key: 'assetName', label: 'Asset', sortable: true, width: '30%' },
    { key: 'priority', label: 'Priority', sortable: true, width: '8%', align: 'center' },
    { key: 'status', label: 'Status', sortable: true, width: '12%', align: 'center' },
    { key: 'assignedTo', label: 'Assigned To', sortable: true, width: '16%' },
    { key: 'dueDate', label: 'Due Date', sortable: true, width: '12%', format: 'date' },
    { key: 'createdAt', label: 'Created', sortable: true, width: '11%', format: 'datetime' },
    { key: 'completedAt', label: 'Completed', sortable: true, width: '11%', format: 'datetime' },
];

const getTableActions = (): TableAction[] => {
    const actions: TableAction[] = [
        { key: 'bulk-assign', label: 'Bulk Assign', icon: faUserCog, variant: 'secondary' },
    ];
    
    // Add "Import Assets" action if no assets are available
    if (!hasAvailableAssets.value) {
        actions.unshift({
            key: 'import-assets',
            label: 'Import Assets',
            icon: faPlus,
            variant: 'primary'
        });
    }
    
    return actions;
};

const rowActions = computed((): TableRowAction<TaskTableRow>[] => {
    const actions: TableRowAction<TaskTableRow>[] = [
        { 
            key: 'open', 
            label: 'Open', 
            icon: faPlay, 
            variant: 'primary',
            disabled: (row: TaskTableRow) => 
                row.status === TaskStatus.COMPLETED || 
                row.status === TaskStatus.ARCHIVED ||
                row.status === TaskStatus.SUSPENDED ||
                (row.status === TaskStatus.DEFERRED && !canManageTasks.value)
        },
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
        
        // Preload assets for visible tasks
        preloadVisibleAssets(projectId.value, tasks.value);
        
    } catch (error) {
        logger.error('Error loading tasks', error);
        errorMessage.value = 'Failed to load tasks';
        handleError(error, 'Loading tasks');
        
        // Fallback to mock data if backend fails
        logger.warn('Falling back to mock data...');
        await loadMockTasks();
    } finally {
        isLoading.value = false;
    }
};

const loadMockTasks = async () => {
    // Mock delay to simulate API call
    await new Promise(resolve => setTimeout(resolve, 800));
    
    // Generate mock data based on stage ID for variety
    const mockTasks: TaskTableRow[] = generateMockTasks(stageId.value);
    
    tasks.value = mockTasks;
    totalItems.value = mockTasks.length;
    
    // Set stage name and description based on common stage types
    const stageNames = {
        1: 'Initial Upload',
        2: 'Annotation',
        3: 'Review',
        4: 'Quality Control',
        5: 'Final Export'
    };
    
    const stageDescriptions = {
        1: 'Assets are uploaded and prepared for annotation',
        2: 'Primary annotation tasks for uploaded assets',
        3: 'Annotation review and validation tasks',
        4: 'Quality control and final validation',
        5: 'Final processing and export preparation'
    };
    
    stageName.value = stageNames[stageId.value as keyof typeof stageNames] || `Stage ${stageId.value}`;
    stageDescription.value = stageDescriptions[stageId.value as keyof typeof stageDescriptions] || 'Manage tasks for this workflow stage';
};

const generateMockTasks = (stageId: number): TaskTableRow[] => {
    const baseAssets = [
        'image_001.jpg', 'image_002.jpg', 'image_003.jpg', 'image_004.jpg', 'image_005.jpg',
        'document_001.pdf', 'scan_001.png', 'photo_001.jpg', 'diagram_001.svg', 'chart_001.png'
    ];
    
    const users = [
        'john.doe@example.com', 
        'jane.smith@example.com', 
        'bob.wilson@example.com', 
        'alice.johnson@example.com',
        null, null // Some unassigned tasks
    ];
    
    const statuses: TaskStatus[] = [
        TaskStatus.NOT_STARTED, 
        TaskStatus.IN_PROGRESS, 
        TaskStatus.COMPLETED,
        TaskStatus.READY_FOR_ANNOTATION,
        TaskStatus.READY_FOR_REVIEW,
        TaskStatus.SUSPENDED,
        TaskStatus.DEFERRED
    ];
    
    return baseAssets.slice(0, Math.min(6, baseAssets.length)).map((asset, index) => {
        const taskId = stageId * 100 + index + 1;
        const status = statuses[index % statuses.length];
        const assignedTo = users[index % users.length];
        
        const createdDate = new Date('2025-07-06');
        createdDate.setHours(createdDate.getHours() - (index * 2));
        
        const task: TaskTableRow = {
            id: taskId,
            assetId: taskId + 1000, // Mock assetId
            assetName: asset,
            priority: Math.floor(Math.random() * 3) + 1, // Random priority 1-3
            status,
            assignedTo: assignedTo || undefined,
            createdAt: createdDate.toISOString(),
            stage: stageName.value
        };
        
        // Add due dates for some tasks
        if (index % 2 === 0) {
            const dueDate = new Date('2025-07-06');
            dueDate.setDate(dueDate.getDate() + (index + 1));
            task.dueDate = dueDate.toISOString().split('T')[0];
        }
        
        // Add completion dates for completed tasks
        if (status === 'COMPLETED') {
            const completedDate = new Date('2025-07-06');
            completedDate.setHours(completedDate.getHours() - index);
            task.completedAt = completedDate.toISOString();
        }
        
        return task;
    });
};

const handleTableAction = (actionKey: string) => {
    logger.debug('Table action triggered', { actionKey });
    switch (actionKey) {
        case 'bulk-assign':
            // TODO: Show bulk assignment dialog
            break;
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
        case 'open':
            handleTaskClick(row, index);
            break;
        case 'edit':
            selectedTask.value = row;
            showEditModal.value = true;
            logger.info('Edit task requested', { taskId: row.id });
            break;
        case 'assign':
            // TODO: Show assignment dialog
            logger.info('Assign task requested', { taskId: row.id });
            break;
        case 'change-status':
            selectedTask.value = row;
            showStatusModal.value = true;
            break;
    }
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

const formatDate = (dateString: string): string => {
    return new Date(dateString).toLocaleDateString();
};

const getDueDateClass = (dueDate: string): string => {
    const due = new Date(dueDate);
    const now = new Date();
    const diffDays = Math.ceil((due.getTime() - now.getTime()) / (1000 * 60 * 60 * 24));
    
    if (diffDays < 0) return 'overdue';
    if (diffDays <= 1) return 'urgent';
    if (diffDays <= 3) return 'soon';
    return 'normal';
};

const getPriorityClass = (priority: number): string => {
    if (priority >= 3) return 'priority-high';
    if (priority === 2) return 'priority-medium';
    return 'priority-low';
};

const getPriorityIcon = (priority: number) => {
    if (priority >= 3) return faExclamationTriangle;
    if (priority === 2) return faArrowUp;
    return faMinus;
};

const getPriorityLabel = (priority: number): string => {
    if (priority >= 3) return 'High';
    if (priority === 2) return 'Medium';
    return 'Low';
};

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

// Custom cell styling
.assigned-user {
    display: flex;
    align-items: center;
    gap: 0.25rem;
    
    .user-icon {
        color: var(--color-success);
    }
}

.unassigned {
    display: flex;
    align-items: center;
    gap: 0.25rem;
    color: var(--color-gray-600);
    font-style: italic;
    
    .unassigned-icon {
        color: var(--color-warning);
    }
}

.due-date {
    display: flex;
    align-items: center;
    gap: 0.25rem;
    
    &.normal {
        color: var(--color-gray-800);
    }
    
    &.soon {
        color: var(--color-warning);
        font-weight: 500;
    }
    
    &.urgent {
        color: var(--color-warning);
        font-weight: 600;
    }
    
    &.overdue {
        color: var(--color-error);
        font-weight: 600;
    }
}

// Asset preview styles
.asset-preview-cell {
    display: flex;
    align-items: center;
    justify-content: center;
    cursor: pointer;
    padding: 0.5rem;
    
    .asset-thumbnail {
        position: relative;
        width: 128px;
        height: 128px;
        border-radius: 8px;
        overflow: hidden;
        border: 2px solid var(--color-gray-300);
        flex-shrink: 0;
        box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
        
        .thumbnail-image {
            width: 100%;
            height: 100%;
            object-fit: cover;
            transition: transform 0.2s;
            background: var(--color-gray-100);
            
            &.loading {
                opacity: 0.6;
            }
            
            &.image-error {
                opacity: 1;
                background: var(--color-gray-200);
            }
            
            // Handle empty src images
            &:not([src]),
            &[src=""] {
                background: var(--color-gray-100);
                opacity: 0.8;
            }
        }
        
        .loading-overlay {
            position: absolute;
            top: 0;
            left: 0;
            right: 0;
            bottom: 0;
            display: flex;
            align-items: center;
            justify-content: center;
            background: var(--color-gray-100);
            border-radius: 6px;
            
            .loading-icon {
                color: var(--color-primary);
                font-size: 1.5rem;
            }
        }
        
        .error-overlay {
            position: absolute;
            top: 0;
            left: 0;
            right: 0;
            bottom: 0;
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            background: var(--color-gray-200);
            border-radius: 6px;
            gap: 0.25rem;
            
            .error-icon {
                color: var(--color-error);
                font-size: 1.25rem;
            }
            
            .error-text {
                color: var(--color-gray-600);
                font-size: 0.75rem;
                font-weight: 500;
                text-align: center;
            }
        }
        
        .annotation-indicator {
            position: absolute;
            top: 4px;
            right: 4px;
            background: rgba(59, 130, 246, 0.9);
            color: white;
            border-radius: 12px;
            padding: 2px 6px;
            font-size: 0.75rem;
            font-weight: 600;
            display: flex;
            align-items: center;
            gap: 2px;
            box-shadow: 0 2px 4px rgba(0, 0, 0, 0.2);
            
            .annotation-icon {
                font-size: 0.7rem;
            }
            
            .annotation-count {
                line-height: 1;
            }
        }
        
        .annotation-overlay {
            position: absolute;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            pointer-events: none;
            z-index: 1;
        }
    }
    
    &:hover {
        .asset-thumbnail {
            border-color: var(--color-primary);
            
            .thumbnail-image:not(.loading) {
                transform: scale(1.02);
            }
        }
    }
}

.asset-preview-popup {
    position: fixed;
    background: var(--color-white);
    border: 1px solid var(--color-gray-300);
    border-radius: 8px;
    box-shadow: 0 10px 25px rgba(0, 0, 0, 0.15);
    padding: 0;
    z-index: 1000;
    max-width: 400px;
    overflow: hidden;
    
    .preview-header {
        padding: 0.75rem 1rem;
        background: var(--color-gray-100);
        border-bottom: 1px solid var(--color-gray-300);
        
        .preview-asset-name {
            font-weight: 500;
            color: var(--color-gray-800);
            font-size: 0.875rem;
            display: block;
            margin-bottom: 0.25rem;
        }
        
        .asset-metadata {
            display: flex;
            gap: 0.75rem;
            font-size: 0.75rem;
            color: var(--color-gray-600);
            
            .asset-size,
            .asset-dimensions,
            .annotation-info {
                display: flex;
                align-items: center;
                gap: 0.25rem;
            }
            
            .annotation-info {
                color: var(--color-primary);
                font-weight: 500;
            }
        }
    }
    
    .preview-image-container {
        padding: 1rem;
        display: flex;
        justify-content: center;
        align-items: center;
        background: var(--color-gray-50);
        min-height: 200px;
        
        .loading-state {
            display: flex;
            flex-direction: column;
            align-items: center;
            gap: 0.75rem;
            color: var(--color-gray-600);
            
            .loading-icon-large {
                font-size: 1.5rem;
                color: var(--color-primary);
            }
        }
        
        .error-state {
            display: flex;
            flex-direction: column;
            align-items: center;
            gap: 0.75rem;
            color: var(--color-gray-600);
            padding: 2rem;
            
            .error-icon-large {
                font-size: 2rem;
                color: var(--color-error);
            }
        }
        
        .preview-image-wrapper {
            position: relative;
            display: flex;
            justify-content: center;
            align-items: center;
        }
        
        .preview-image {
            max-width: 100%;
            max-height: 300px;
            border-radius: 4px;
            box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
            background: var(--color-gray-100);
            min-width: 200px;
            min-height: 150px;
            
            &.image-error {
                opacity: 1;
                background: var(--color-gray-200);
            }
            
            // Handle empty src images
            &:not([src]),
            &[src=""] {
                background: var(--color-gray-100);
                opacity: 0.9;
            }
        }
        
        .preview-annotation-overlay {
            position: absolute;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            pointer-events: none;
            z-index: 1;
        }
    }
}

.no-due-date {
    color: var(--color-gray-600);
    font-style: italic;
}

.priority-cell {
    display: flex;
    align-items: center;
    gap: 0.25rem;
    font-weight: 500;
    
    &.priority-high {
        color: var(--color-error);
    }
    
    &.priority-medium {
        color: var(--color-warning);
    }
    
    &.priority-low {
        color: var(--color-gray-600);
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