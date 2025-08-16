<template>
    <div class="dashboard-grid-wrapper">
        <div v-if="editMode" class="add-widget-zone">
            <Button @click="showAddWidgetModal = true" variant="primary" class="add-widget-btn">
                <font-awesome-icon :icon="faPlus" />
                Add Widget
            </Button>
        </div>

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
import { FontAwesomeIcon } from "@fortawesome/vue-fontawesome";
import { faPlus } from "@fortawesome/free-solid-svg-icons";

// Gridstack imports
import { GridStack } from 'gridstack';
import type { GridStackNode } from 'gridstack';
import 'gridstack/dist/gridstack.min.css';

import WidgetContainer from "./WidgetContainer.vue";
import AddWidgetModal from "./AddWidgetModal.vue";
import WidgetSettingsModal from "./WidgetSettingsModal.vue";
import Button from "@/components/common/Button.vue";
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
    editMode?: boolean;
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
    editMode: false,
    gridColumns: 12,
    gridRowHeight: 100,
    gridGap: 16,
});
const emit = defineEmits<Emits>();

const gridstackContainer = ref<HTMLElement>();
const grid = ref<GridStack | null>(null);

const selectedWidgetId = ref<string | null>(null);
const showAddWidgetModal = ref(false);
const showSettingsModal = ref(false);
const widgetData = reactive<Record<string, any>>({});
const widgetLoadingStates = reactive<Record<string, boolean>>({});
const widgetErrors = reactive<Record<string, string | null>>({});
const widgetRefreshingStates = reactive<Record<string, boolean>>({});

const selectedWidget = computed(() =>
    selectedWidgetId.value
        ? props.widgets.find((w) => w.widgetId === selectedWidgetId.value)
        : null
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
        float: true, // Allows widgets to float up to fill empty space
        disableDrag: !props.editMode,
        disableResize: !props.editMode,
    });

    // Add event listeners to sync Gridstack changes back to the parent
    grid.value.on('change', (_event, items: GridStackNode[]) => {
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
});

onUnmounted(() => {
    grid.value?.destroy();
});


watch(() => props.editMode, (isEditable) => {
    if (grid.value) {
        grid.value.enableMove(isEditable);
        grid.value.enableResize(isEditable);
    }
});

// Watch for widgets being added or removed from the parent
watch(() => props.widgets, async (newWidgets, oldWidgets) => {
    if (!grid.value) return;

    const newIds = new Set(newWidgets.map(w => w.widgetId));
    const oldIds = new Set(oldWidgets.map(w => w.widgetId));

    // Handle removals
    for (const widget of oldWidgets) {
        if (!newIds.has(widget.widgetId)) {
            const el = grid.value.engine.nodes.find(n => n.id === widget.widgetId)?.el;
            if (el) {
                grid.value.removeWidget(el, false); // `false` to prevent DOM removal, Vue will do it
            }
        }
    }
    
    // Handle additions
    const addedWidgets = newWidgets.filter(w => !oldIds.has(w.widgetId));
    if (addedWidgets.length > 0) {
        // Wait for Vue to render the new widget elements in the DOM
        await nextTick();
        addedWidgets.forEach(widget => {
            const el = gridstackContainer.value?.querySelector(`[gs-id="${widget.widgetId}"]`);
            if (el && grid.value && !grid.value.engine.nodes.some(n => n.id === widget.widgetId)) {
                grid.value.makeWidget(el as HTMLElement);
            }
        });
    }
}, { deep: false });


const getWidgetComponent = (widgetType: string) => widgetComponents[widgetType] || "div";
const getWidgetDefinition = (widgetType: string) => props.widgetDefinitions.find((def) => def.widgetType === widgetType);

const handleAddWidget = ({ widgetType, title }: { widgetType: string; title?: string; }) => {
    emit("widget-add", { widgetType, title });
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
});
</script>

<style scoped>
/* You may need to adjust some styles, but most should work as before */
.dashboard-grid-wrapper {
    position: relative;
    width: 100%;
}

/* Add some default styling for Gridstack placeholders */
.grid-stack .grid-stack-placeholder > .placeholder-content {
    border: 2px dashed var(--color-primary);
    background-color: rgba(var(--color-primary-rgb), 0.1);
    border-radius: 0.5rem;
}

.add-widget-zone {
    /* Styles are the same as before */
    position: sticky;
    top: 0;
    z-index: 100;
    display: flex;
    justify-content: center;
    padding: 1rem 0;
    margin-bottom: 1.5rem;
    background: rgba(255, 255, 255, 0.9);
    backdrop-filter: blur(12px);
    border-radius: 0.75rem;
}

.add-widget-btn {
    display: flex;
    align-items: center;
    gap: 0.5rem;
}
</style>