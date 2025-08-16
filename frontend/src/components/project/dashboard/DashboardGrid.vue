<template>
    <div class="dashboard-grid-wrapper">
        <!-- Empty State -->
        <div v-if="widgets.length === 0" class="empty-dashboard">
            <div class="empty-icon">
                <svg width="64" height="64" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                    <path d="M3 3h18v18H3V3zm2 2v14h14V5H5zm3 3h8v2H8V8zm0 4h6v2H8v-2z" fill="currentColor" opacity="0.3"/>
                    <path d="M19 13v8H5v-8M3 3h18v10H3V3z" stroke="currentColor" stroke-width="2" fill="none"/>
                    <circle cx="17" cy="17" r="3" fill="none" stroke="currentColor" stroke-width="2"/>
                    <path d="M19 17l2 2" stroke="currentColor" stroke-width="2"/>
                </svg>
            </div>
            <h3 class="empty-title">Start Building Your Dashboard</h3>
            <p class="empty-description">
                Add widgets to track your team's progress, monitor project health, and stay on top of important metrics.
            </p>
        </div>

        <!-- Grid Container -->
        <div v-show="widgets.length > 0" ref="gridstackContainer" class="grid-stack">
            <WidgetContainer
                v-for="widget in widgets"
                :key="widget.widgetId"
                :widget="widget"
                :gs-id="widget.widgetId"
                :gs-x="widget.gridX"
                :gs-y="widget.gridY"
                :gs-w="widget.gridWidth"
                :gs-h="widget.gridHeight"
                :selected="selectedWidgetId === widget.widgetId"
                :loading="widgetLoadingStates[widget.widgetId]"
                :error="widgetErrors[widget.widgetId]"
                :refreshing="widgetRefreshingStates[widget.widgetId]"
                @select="handleWidgetSelect(widget.widgetId)"
                @settings="handleWidgetSettings(widget.widgetId)"
                @refresh="handleWidgetRefresh(widget.widgetId)"
                @remove="handleWidgetRemove(widget.widgetId)"
                @retry="handleWidgetRetry(widget.widgetId)"
            >
                <component
                    :is="getWidgetComponent(widget.widgetType)"
                    :widget="widget"
                    :data="widgetData[widget.widgetId]"
                    :loading="widgetLoadingStates[widget.widgetId]"
                    :error="widgetErrors[widget.widgetId]"
                    @configure="handleWidgetConfigure(widget.widgetId)"
                />
            </WidgetContainer>
        </div>

        <AddWidgetModal
            v-if="showAddWidgetModal"
            :widget-definitions="widgetDefinitions"
            @add="handleAddWidget"
            @close="showAddWidgetModal = false"
        />
        <WidgetSettingsModal
            v-if="showSettingsModal && selectedWidget"
            :widget="selectedWidget"
            :widget-definition="getWidgetDefinition(selectedWidget.widgetType)"
            @save="handleWidgetSettingsSave"
            @close="showSettingsModal = false"
        />
    </div>
</template>

<script setup lang="ts">
import { computed, ref, reactive, onMounted, onUnmounted, watch, nextTick } from "vue";

// Gridstack imports
import { GridStack } from 'gridstack';
import 'gridstack/dist/gridstack.min.css';

import WidgetContainer from "./WidgetContainer.vue";
import AddWidgetModal from "./AddWidgetModal.vue";
import WidgetSettingsModal from "./WidgetSettingsModal.vue";
import type { WidgetInstanceDto, WidgetDefinitionDto } from "@/types/dashboard/dashboard";

// Widget Components (same as before)
import ProjectHealthWidget from "./widgets/ProjectHealthWidget.vue";
import WorkflowProgressWidget from "./widgets/WorkflowProgressWidget.vue";
import UserPerformanceWidget from "./widgets/UserPerformanceWidget.vue";
import TaskStatisticsWidget from "./widgets/TaskStatisticsWidget.vue";
import RecentActivitiesWidget from "./widgets/RecentActivitiesWidget.vue";


// Props and Emits are unchanged
interface Props {
    widgets: WidgetInstanceDto[];
    widgetDefinitions: WidgetDefinitionDto[];
    gridColumns?: number;
    gridRowHeight?: number;
    gridGap?: number;
}
interface Emits {
    (e: "widget-move", payload: { widgetId: string; x: number; y: number }): void;
    (e: "widget-resize", payload: { widgetId: string; width: number; height: number }): void;
    (e: "widget-add", payload: { widgetType: string; title?: string }): void;
    (e: "widget-remove", payload: { widgetId: string }): void;
    (e: "widget-settings-save", payload: { widgetId: string; settings: Record<string, any> }): void;
    (e: "widget-refresh", payload: { widgetId: string }): void;
    (e: "layout-change"): void;
}
const props = withDefaults(defineProps<Props>(), {
    gridColumns: 12,
    gridRowHeight: 100,
    gridGap: 8,
});
const emit = defineEmits<Emits>();

const gridstackContainer = ref<HTMLElement>();
const grid = ref<GridStack | null>(null);
// Initialize isHidden based on whether we have widgets initially
const isHidden = ref(true);

const selectedWidgetId = ref<string | null>(null);
const showAddWidgetModal = ref(false);
const showSettingsModal = ref(false);
const widgetData = reactive<Record<string, any>>({});
const widgetLoadingStates = reactive<Record<string, boolean>>({});
const widgetErrors = reactive<Record<string, string | null>>({});
const widgetRefreshingStates = reactive<Record<string, boolean>>({});

const selectedWidget = computed(() =>
    selectedWidgetId.value ? props.widgets.find((w) => w.widgetId === selectedWidgetId.value) : null
);
const widgetComponents: Record<string, any> = {
    project_health: ProjectHealthWidget,
    workflow_progress: WorkflowProgressWidget,
    user_performance: UserPerformanceWidget,
    task_statistics: TaskStatisticsWidget,
    recent_activities: RecentActivitiesWidget,
};

// Initialize GridStack with common configuration and event listeners
const initializeGridStack = async () => {
    if (grid.value) return; // Already initialized
    
    // Ensure container is ready
    await nextTick();
    
    if (!gridstackContainer.value) {
        console.warn('GridStack container not found, skipping initialization');
        return;
    }
    
    grid.value = GridStack.init({
        column: props.gridColumns,
        cellHeight: props.gridRowHeight,
        margin: props.gridGap,
        float: true, // Allow widgets to float
        disableDrag: false,
        disableResize: false,
        animate: true,
        minRow: 0, // Allow grid to shrink when items are removed
        acceptWidgets: true,
        placeholderClass: 'grid-stack-placeholder', // Custom placeholder styling
        resizable: { handles: 'all' }, // Allow resizing from all corners/edges
        draggable: {
            handle: '.widget-header', // Only drag from the header
            scroll: true, // Enable auto-scroll when dragging near edges
            appendTo: 'parent' // Keep within grid boundaries
        }
    }, gridstackContainer.value);

    // Add event listeners only if grid was successfully created
    if (!grid.value) {
        console.error('Failed to initialize GridStack');
        return;
    }

    // Handle all move/resize completions
    grid.value.on('change', (_event, items) => {
        if (isHidden.value) return;

        items.forEach(item => {
            const widgetId = item.id as string;
            const widgetInStore = props.widgets.find(w => w.widgetId === widgetId);

            if (!widgetInStore) return;

            // Position changed
            if (widgetInStore.gridX !== item.x || widgetInStore.gridY !== item.y) {
                emit("widget-move", { widgetId, x: item.x!, y: item.y! });
            }

            // Size changed
            if (widgetInStore.gridWidth !== item.w || widgetInStore.gridHeight !== item.h) {
                emit("widget-resize", { widgetId, width: item.w!, height: item.h! });
            }
        });

        emit("layout-change");
    });
};

onMounted(async () => {
    // Initialize isHidden based on initial widgets
    if (props.widgets.length > 0) {
        isHidden.value = false;
    }
    
    // The container element always exists now, so we can initialize immediately
    await initializeGridStack();
});

onUnmounted(() => {
    grid.value?.destroy();
});

// Keep track of current widget IDs to detect changes
const currentWidgetIds = ref<Set<string>>(new Set());

// Watch for widgets being added or removed from the parent
watch(() => props.widgets, async (newWidgets) => {
    // Grid must exist to continue
    if (!grid.value) return;
    
    // Set hidden state based on whether we have widgets
    if (newWidgets.length === 0 && !isHidden.value) {
        isHidden.value = true;
    }
    else if (newWidgets.length > 0 && isHidden.value) {
        isHidden.value = false;
    }

    const newIds = new Set(newWidgets.map(w => w.widgetId));
    const oldIds = currentWidgetIds.value;

    // Handle removals first
    const removedWidgetIds = Array.from(oldIds).filter(id => !newIds.has(id));
    
    for (const widgetId of removedWidgetIds) {
        const el = grid.value.engine.nodes.find(n => n.id === widgetId)?.el;
        if (el) {
            grid.value.removeWidget(el, false); // `false` to prevent DOM removal, Vue will handle it
        }
    }
    
    // Handle additions using GridStack's makeWidget method
    const addedWidgetIds = newWidgets.filter(w => !oldIds.has(w.widgetId)).map(w => w.widgetId);
    
    for (const widgetId of addedWidgetIds) {
        // Wait for Vue to render the new widget element
        await nextTick();
        
        const el = gridstackContainer.value?.querySelector(`[gs-id="${widgetId}"]`);
        
        if (el && grid.value && !grid.value.engine.nodes.some(n => n.id === widgetId)) {
            grid.value.makeWidget(el as HTMLElement);
        }
    }

    // Update our tracking set
    currentWidgetIds.value = newIds;

    // Emit layout change after widget additions/removals
    if (addedWidgetIds.length > 0 || removedWidgetIds.length > 0) {
        await nextTick();
        emit("layout-change");
    }

    // Show grid after widgets are loaded and positioned
    if (isHidden.value && newWidgets.length > 0) {
        await nextTick();
        setTimeout(() => {
            isHidden.value = false;
        }, 100);
    }
}, { deep: true });

// Initialize the tracking set when component mounts
watch(() => props.widgets, (widgets) => {
    currentWidgetIds.value = new Set(widgets.map(w => w.widgetId));
}, { immediate: true });

const getWidgetComponent = (widgetType: string) => widgetComponents[widgetType] || "div";
const getWidgetDefinition = (widgetType: string) => props.widgetDefinitions.find((def) => def.widgetType === widgetType);

const handleAddWidget = ({ widgetTypes }: { widgetTypes: string[] }) => {
    // Emit add event for each selected widget type
    widgetTypes.forEach(widgetType => {
        emit("widget-add", { widgetType });
    });
    showAddWidgetModal.value = false;
};

const handleWidgetRemove = (widgetId: string) => {
    emit("widget-remove", { widgetId });
};

const handleWidgetSelect = (widgetId: string) => { selectedWidgetId.value = selectedWidgetId.value === widgetId ? null : widgetId; };
const handleWidgetSettings = (widgetId: string) => { selectedWidgetId.value = widgetId; showSettingsModal.value = true; };
const handleWidgetConfigure = (widgetId: string) => { handleWidgetSettings(widgetId); };
const handleWidgetSettingsSave = ({ settings }: { settings: Record<string, any>; }) => {
    if (selectedWidgetId.value) {
        emit("widget-settings-save", { widgetId: selectedWidgetId.value, settings, });
        showSettingsModal.value = false;
    }
};
const handleWidgetRefresh = async (widgetId: string) => {
    widgetRefreshingStates[widgetId] = true;
    try {
        emit("widget-refresh", { widgetId });
    } finally {
        widgetRefreshingStates[widgetId] = false;
    }
};
const handleWidgetRetry = (widgetId: string) => {
    widgetErrors[widgetId] = null;
    handleWidgetRefresh(widgetId);
};

defineExpose({
    setWidgetData: (widgetId: string, data: any) => { widgetData[widgetId] = data; },
    setWidgetLoading: (widgetId: string, loading: boolean) => { widgetLoadingStates[widgetId] = loading; },
    setWidgetError: (widgetId: string, error: string | null) => { widgetErrors[widgetId] = error; },
    openAddWidgetModal: () => { showAddWidgetModal.value = true; },
});
</script>

<style scoped>
.dashboard-grid-wrapper {
    position: relative;
    width: 100%;
    min-height: 200px;
}

.grid-stack {
    background: #fafbfc;
    border-radius: 0.5rem;
    padding: 1rem;
    position: relative;
    
    /* Subtle grid pattern for better visual feedback */
    background-image: 
        linear-gradient(to right, rgba(0, 0, 0, 0.03) 1px, transparent 1px),
        linear-gradient(to bottom, rgba(0, 0, 0, 0.03) 1px, transparent 1px);
    background-size: 20px 20px;
}

/* Enhanced GridStack placeholder styling for better visual feedback */
.grid-stack .grid-stack-placeholder {
    background: rgba(59, 130, 246, 0.1) !important;
    border: 2px dashed rgba(59, 130, 246, 0.4) !important;
    border-radius: 0.75rem !important;
    opacity: 0.8 !important;
    transition: all 0.2s ease !important;
}

.grid-stack .grid-stack-placeholder.ui-resizable-resizing {
    opacity: 1 !important;
    background: rgba(59, 130, 246, 0.15) !important;
    border-color: rgba(59, 130, 246, 0.6) !important;
}

/* Ensure smooth transitions for floating widgets */
.grid-stack .grid-stack-item {
    transition: transform 0.3s ease, opacity 0.2s ease !important;
}

.grid-stack .grid-stack-item.ui-draggable-dragging {
    opacity: 0.8 !important;
    transform: scale(1.02) !important;
    z-index: 1000 !important;
}

/* Empty Dashboard State */
.empty-dashboard {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    min-height: 400px;
    padding: 3rem 2rem;
    text-align: center;
    background: var(--color-white);
    border-radius: 0.75rem;
    border: 2px dashed var(--color-border-light);
    color: var(--color-text-secondary);
}

.empty-icon {
    margin-bottom: 1.5rem;
    color: var(--color-text-tertiary);
    opacity: 0.6;
}

.empty-title {
    margin: 0 0 1rem 0;
    font-size: 1.5rem;
    font-weight: 600;
    color: var(--color-text-primary);
}

.empty-description {
    margin: 0 0 2rem 0;
    font-size: 1rem;
    line-height: 1.5;
    color: var(--color-text-secondary);
    max-width: 400px;
}


/* Responsive adjustments for empty state */
@media (max-width: 768px) {
    .empty-dashboard {
        min-height: 300px;
        padding: 2rem 1rem;
    }
    
    .empty-title {
        font-size: 1.25rem;
    }
    
    .empty-description {
        font-size: 0.875rem;
    }
}
</style>