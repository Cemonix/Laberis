<template>
    <div class="dashboard-grid-wrapper">

        <div ref="gridstackContainer" class="grid-stack">
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
import type { GridStackNode } from 'gridstack';
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
const isInitializing = ref(true);

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

onMounted(() => {
    grid.value = GridStack.init({
        column: props.gridColumns,
        cellHeight: props.gridRowHeight,
        margin: props.gridGap,
        float: true,
        disableDrag: false,
        disableResize: false,
        animate: true,
        minRow: 0, // Allow grid to shrink when items are removed
        acceptWidgets: true,
        placeholderClass: 'grid-stack-placeholder', // Custom placeholder styling
        resizable: {
            handles: 'all' // Allow resizing from all corners/edges
        },
        draggable: {
            handle: '.widget-header', // Only drag from the header
            scroll: true, // Enable auto-scroll when dragging near edges
            appendTo: 'parent' // Keep within grid boundaries
        }
    });

    // Add event listeners to sync Gridstack changes back to the parent
    grid.value.on('change', (_event, items: GridStackNode[]) => {
        // Don't emit events during initialization to avoid conflicts
        if (isInitializing.value) return;
        
        items.forEach((item) => {
            const widgetId = item.id as string;
            
            // Check if position or size actually changed to avoid redundant emits
            const widgetInStore = props.widgets.find(w => w.widgetId === widgetId);
            if (!widgetInStore) return;
            
            if (widgetInStore.gridX !== item.x || widgetInStore.gridY !== item.y) {
                emit("widget-move", { widgetId, x: item.x!, y: item.y! });
            }
            if(widgetInStore.gridWidth !== item.w || widgetInStore.gridHeight !== item.h) {
                emit("widget-resize", { widgetId, width: item.w!, height: item.h! });
            }
        });
        emit("layout-change");
    });

    // Add event listeners for drag/resize events to trigger compaction
    grid.value.on('dragstop', () => {
        nextTick(() => {
            if (grid.value) {
                grid.value.compact();
            }
        });
    });

    grid.value.on('resizestop', () => {
        nextTick(() => {
            if (grid.value) {
                grid.value.compact();
            }
        });
    });

    grid.value.on('removed', () => {
        nextTick(() => {
            if (grid.value) {
                grid.value.compact();
            }
        });
    });
});

onUnmounted(() => {
    grid.value?.destroy();
});

// Keep track of current widget IDs to detect changes
const currentWidgetIds = ref<Set<string>>(new Set());

// Watch for widgets being added or removed from the parent
watch(() => props.widgets, async (newWidgets) => {
    if (!grid.value) return;
    
    // Complete initialization after widgets are loaded and positioned
    if (isInitializing.value && newWidgets.length > 0) {
        await nextTick();
        // Allow one frame for GridStack to position widgets, then enable change tracking
        setTimeout(() => {
            isInitializing.value = false;
        }, 100);
    }
    
    // If widgets list becomes empty (new dashboard loading), reset initialization flag
    if (newWidgets.length === 0 && !isInitializing.value) {
        isInitializing.value = true;
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
        
        if (el && !grid.value.engine.nodes.some(n => n.id === widgetId)) {
            grid.value.makeWidget(el as HTMLElement);
        }
    }

    // Update our tracking set
    currentWidgetIds.value = newIds;

    // Trigger compaction after changes
    if (addedWidgetIds.length > 0 || removedWidgetIds.length > 0) {
        await nextTick();
        if (grid.value) {
            grid.value.compact();
        }
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
</style>