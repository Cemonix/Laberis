<template>
    <div class="project-dashboard">
        <!-- Dashboard Header -->
        <div class="dashboard-header">
            <div class="header-content">
                <div class="header-main">
                    <h1 class="dashboard-title">Project Dashboard</h1>
                    <div
                        class="project-info"
                        v-if="projectStore.currentProject"
                    >
                        <span class="project-name">{{
                            projectStore.currentProject.name
                        }}</span>
                        <span
                            class="last-updated"
                            v-if="dashboardStore.lastRefresh"
                        >
                            Last updated: {{ formatLastRefresh() }}
                        </span>
                    </div>
                </div>

                <div class="header-actions">
                    <Button
                        @click="openAddWidget"
                        variant="primary"
                    >
                        <font-awesome-icon :icon="faPlus" />
                        Add Widget
                    </Button>

                    <Button
                        @click="refreshDashboard"
                        :disabled="refreshing"
                        variant="secondary"
                    >
                        <font-awesome-icon
                            :icon="faRefresh"
                            :class="{ rotating: refreshing }"
                        />
                        Refresh
                    </Button>

                    <Button
                        @click="openSettings"
                        variant="secondary"
                        title="Dashboard Settings"
                    >
                        <font-awesome-icon :icon="faCog" />
                    </Button>
                </div>
            </div>
        </div>

        <!-- Dashboard Content -->
        <div class="dashboard-content">
            <!-- Loading State -->
            <div v-if="isLoading" class="dashboard-loading">
                <div class="loading-spinner"></div>
                <span>Loading dashboard...</span>
            </div>

            <!-- Error State -->
            <div v-else-if="dashboardStore.error" class="dashboard-error">
                <font-awesome-icon :icon="faExclamationCircle" />
                <h3>Dashboard Error</h3>
                <p>{{ dashboardStore.error }}</p>
                <Button @click="retryLoad" variant="primary">
                    <font-awesome-icon :icon="faRefresh" />
                    Retry
                </Button>
            </div>

            <!-- Dashboard Grid -->
            <DashboardGrid
                v-else
                ref="dashboardGridRef"
                :widgets="dashboardStore.layout?.widgets || []"
                :widget-definitions="dashboardStore.widgetDefinitions"
                :auto-refresh="autoRefresh"
                :refresh-interval="refreshInterval"
                @widget-move="handleWidgetMove"
                @widget-resize="handleWidgetResize"
                @widget-add="handleWidgetAdd"
                @widget-remove="handleWidgetRemove"
                @widget-settings-save="handleWidgetSettingsSave"
                @widget-refresh="handleWidgetRefresh"
                @layout-change="handleLayoutChange"
            />
        </div>

        <!-- Dashboard Settings Modal -->
        <DashboardSettingsModal
            v-if="showSettingsModal"
            :auto-refresh="autoRefresh"
            :refresh-interval="refreshInterval"
            :theme="dashboardStore.currentTheme"
            @save="handleSettingsSave"
            @close="showSettingsModal = false"
        />
    </div>
</template>

<script setup lang="ts">
import { computed, ref, onMounted, onUnmounted, watch } from "vue";
import { useRouter, useRoute } from "vue-router";
import { FontAwesomeIcon } from "@fortawesome/vue-fontawesome";
import {
    faPlus,
    faRefresh,
    faCog,
    faExclamationCircle
} from "@fortawesome/free-solid-svg-icons";
import DashboardGrid from "@/components/project/dashboard/DashboardGrid.vue";
import DashboardSettingsModal from "@/components/project/dashboard/DashboardSettingsModal.vue";
import Button from "@/components/common/Button.vue";
import { useDashboardStore } from "@/stores/dashboardStore";
import { useProjectStore } from "@/stores/projectStore";
import { useErrorHandler } from "@/composables/useErrorHandler";
import { AppLogger } from "@/core/logger/logger";

// Props (from router)
interface Props {
    projectId?: string;
}

const props = defineProps<Props>();
const logger = AppLogger.createComponentLogger("ProjectDashboard");

// Stores
const dashboardStore = useDashboardStore();
const projectStore = useProjectStore();
const router = useRouter();
const route = useRoute();
const { handleError } = useErrorHandler();

// Component refs
const dashboardGridRef = ref<InstanceType<typeof DashboardGrid>>();

// Component state
const refreshing = ref(false);
const showSettingsModal = ref(false);
const autoRefresh = ref(true);
const refreshInterval = ref(300000); // 5 minutes

// Auto-refresh timer
const autoRefreshTimer = ref<NodeJS.Timeout | null>(null);

// Computed properties
const isLoading = computed(
    () => dashboardStore.loading || projectStore.loading
);

const currentProject = computed(() => projectStore.currentProject);

const projectId = computed(() => {
    const paramId = props.projectId || (route.params.projectId as string);
    const id = paramId ? parseInt(paramId, 10) : null;
    if (id) {
        logger.debug("Project ID computed", { paramId, id, source: props.projectId ? 'props' : 'route' });
    }
    return id;
});

// Lifecycle and watchers
watch(
    currentProject,
    async (project, oldProject) => {
        // Only reinitialize if the project actually changed
        if (project && (!oldProject || oldProject.id !== project.id)) {
            logger.info("Project changed, reinitializing dashboard", { 
                newProjectId: project.id, 
                oldProjectId: oldProject?.id 
            });
            await initializeDashboard(project.id);
        }
    },
    { immediate: false } // Don't run immediately since onMounted handles initial load
);

// Watch for route parameter changes
watch(
    projectId,
    async (newProjectId, oldProjectId) => {
        if (newProjectId && newProjectId !== oldProjectId) {
            logger.info("Project ID from route changed", { newProjectId, oldProjectId });
            try {
                if (!projectStore.currentProject || projectStore.currentProject.id !== newProjectId) {
                    await projectStore.setCurrentProject(newProjectId);
                }
                await initializeDashboard(newProjectId);
            } catch (error) {
                logger.error("Failed to load project from route change", error);
                handleError(error, "Failed to load project");
            }
        }
    }
);

onMounted(async () => {
    logger.info("Dashboard component mounted");

    // Check if we have a project ID from route
    if (projectId.value) {
        logger.info("Loading project from route", { projectId: projectId.value });
        try {
            // Load project if not already loaded
            if (!projectStore.currentProject || projectStore.currentProject.id !== projectId.value) {
                await projectStore.setCurrentProject(projectId.value);
            }
            
            // Initialize dashboard
            await initializeDashboard(projectId.value);

            // Start auto-refresh if enabled
            if (autoRefresh.value) {
                startAutoRefresh();
            }
            return;
        } catch (error) {
            logger.error("Failed to load project", error);
            handleError(error, "Failed to load project");
        }
    }

    // Fallback: Check if we have a current project
    if (projectStore.currentProject) {
        logger.info("Using current project from store");
        await initializeDashboard(projectStore.currentProject.id);
        
        if (autoRefresh.value) {
            startAutoRefresh();
        }
        return;
    }

    // No project available, redirect to projects list
    logger.warn("No current project available, redirecting to projects");
    router.push("/projects");
});

onUnmounted(async () => {
    stopAutoRefresh();
    
    // Clear the debounced save timeout and save immediately if pending
    if (saveLayoutTimeout) {
        clearTimeout(saveLayoutTimeout);
        try {
            // Wait for save to complete before clearing state
            await saveDashboardLayout();
        } catch (error) {
            // Log error but don't throw to prevent unmount issues
            logger.error("Failed to save layout on unmount", error);
        }
    }
    
    dashboardStore.clearState();
});

// Methods
const initializeDashboard = async (projectId: number): Promise<void> => {
    try {
        logger.info("Initializing dashboard", { projectId });
        await dashboardStore.initializeDashboard(projectId);

        // Load widget data
        await loadAllWidgetData();

        logger.info("Dashboard initialized successfully");
    } catch (error) {
        logger.error("Failed to initialize dashboard", error);
        handleError(error, "Dashboard Initialization");
    }
};

const loadAllWidgetData = async (): Promise<void> => {
    if (!dashboardStore.layout?.widgets?.length) return;

    try {
        const promises = dashboardStore.layout.widgets.map(async (widget) => {
            try {
                dashboardGridRef.value?.setWidgetLoading(widget.widgetId, true);

                const data = await dashboardStore.getWidgetData({
                    widgetType: widget.widgetType,
                    customFilters: widget.settings,
                });

                dashboardGridRef.value?.setWidgetData(
                    widget.widgetId,
                    data.data
                );
                dashboardGridRef.value?.setWidgetError(widget.widgetId, null);
            } catch (error) {
                logger.error(
                    `Failed to load data for widget ${widget.widgetId}`,
                    error
                );
                dashboardGridRef.value?.setWidgetError(
                    widget.widgetId,
                    "Failed to load widget data"
                );
            } finally {
                dashboardGridRef.value?.setWidgetLoading(
                    widget.widgetId,
                    false
                );
            }
        });

        await Promise.allSettled(promises);
    } catch (error) {
        logger.error("Failed to load widget data", error);
    }
};

const refreshDashboard = async (): Promise<void> => {
    if (refreshing.value) return;

    refreshing.value = true;
    try {
        logger.info("Refreshing dashboard");
        await dashboardStore.refreshAnalytics();
        await loadAllWidgetData();
        logger.info("Dashboard refreshed successfully");
    } catch (error) {
        logger.error("Failed to refresh dashboard", error);
        handleError(error, "Dashboard Refresh");
    } finally {
        refreshing.value = false;
    }
};

const retryLoad = async (): Promise<void> => {
    if (!currentProject.value) return;
    await initializeDashboard(currentProject.value.id);
};

const openAddWidget = (): void => {
    dashboardGridRef.value?.openAddWidgetModal();
};

const saveDashboardLayout = async (): Promise<void> => {
    if (!currentProject.value) return;
    
    // Check if we have a layout to save
    if (!dashboardStore.layout) {
        logger.debug("No layout to save, skipping save operation");
        return;
    }

    try {
        await dashboardStore.saveConfiguration(currentProject.value.id);
        logger.info("Dashboard layout saved successfully");
    } catch (error) {
        logger.error("Failed to save dashboard layout", error);
        handleError(error, "Dashboard Layout Save");
    }
};

const openSettings = (): void => {
    showSettingsModal.value = true;
};

// Widget event handlers
const handleWidgetMove = ({
    widgetId,
    x,
    y,
}: {
    widgetId: string;
    x: number;
    y: number;
}): void => {
    dashboardStore.updateWidget(widgetId, { gridX: x, gridY: y });
    logger.debug("Widget moved", { widgetId, x, y });
};

const handleWidgetResize = ({
    widgetId,
    width,
    height,
}: {
    widgetId: string;
    width: number;
    height: number;
}): void => {
    dashboardStore.updateWidget(widgetId, {
        gridWidth: width,
        gridHeight: height,
    });
    logger.debug("Widget resized", { widgetId, width, height });
};

const handleWidgetAdd = ({
    widgetType,
    title,
}: {
    widgetType: string;
    title?: string;
}): void => {
    dashboardStore.addWidget(widgetType, title);
    logger.info("Widget added", { widgetType, title });

    // Load data for the new widget
    setTimeout(async () => {
        const widgets = dashboardStore.layout?.widgets || [];
        const newWidget = widgets[widgets.length - 1];
        if (newWidget) {
            await loadWidgetData(newWidget);
        }
    }, 100);
};

const handleWidgetRemove = ({ widgetId }: { widgetId: string }): void => {
    dashboardStore.removeWidget(widgetId);
    logger.info("Widget removed", { widgetId });
};

const handleWidgetSettingsSave = ({
    widgetId,
    settings,
}: {
    widgetId: string;
    settings: Record<string, any>;
}): void => {
    dashboardStore.updateWidget(widgetId, settings);
    logger.info("Widget settings saved", { widgetId, settings });

    // Reload widget data with new settings
    const widget = dashboardStore.layout?.widgets.find(
        (w) => w.widgetId === widgetId
    );
    if (widget) {
        loadWidgetData(widget);
    }
};

const handleWidgetRefresh = async ({
    widgetId,
}: {
    widgetId: string;
}): Promise<void> => {
    const widget = dashboardStore.layout?.widgets.find(
        (w) => w.widgetId === widgetId
    );
    if (widget) {
        await loadWidgetData(widget);
    }
};

// Debounced save to prevent excessive API calls during drag operations
let saveLayoutTimeout: ReturnType<typeof setTimeout> | null = null;

const debouncedSaveLayout = (): void => {
    if (saveLayoutTimeout) {
        clearTimeout(saveLayoutTimeout);
    }
    
    saveLayoutTimeout = setTimeout(() => {
        saveDashboardLayout();
    }, 500); // Wait 500ms after last change before saving
};

const handleLayoutChange = (): void => {
    // Use debounced save for layout changes
    debouncedSaveLayout();
};

const loadWidgetData = async (widget: any): Promise<void> => {
    try {
        dashboardGridRef.value?.setWidgetLoading(widget.widgetId, true);

        const data = await dashboardStore.getWidgetData({
            widgetType: widget.widgetType,
            customFilters: widget.settings,
        });

        dashboardGridRef.value?.setWidgetData(widget.widgetId, data.data);
        dashboardGridRef.value?.setWidgetError(widget.widgetId, null);
    } catch (error) {
        logger.error(
            `Failed to load data for widget ${widget.widgetId}`,
            error
        );
        dashboardGridRef.value?.setWidgetError(
            widget.widgetId,
            "Failed to load widget data"
        );
    } finally {
        dashboardGridRef.value?.setWidgetLoading(widget.widgetId, false);
    }
};

// Settings handlers
const handleSettingsSave = (settings: any): void => {
    autoRefresh.value = settings.autoRefresh;
    refreshInterval.value = settings.refreshInterval;

    if (dashboardStore.layout) {
        dashboardStore.layout.theme = settings.theme;
        dashboardStore.layout.refreshIntervalSeconds =
            settings.refreshInterval / 1000;
    }

    // Update auto-refresh
    if (autoRefresh.value) {
        startAutoRefresh();
    } else {
        stopAutoRefresh();
    }

    showSettingsModal.value = false;
    saveDashboardLayout();

    logger.info("Dashboard settings saved", settings);
};

// Auto-refresh management
const startAutoRefresh = (): void => {
    stopAutoRefresh();

    if (refreshInterval.value > 0) {
        autoRefreshTimer.value = setInterval(() => {
            if (!refreshing.value) {
                refreshDashboard();
            }
        }, refreshInterval.value);

        logger.info("Auto-refresh started", {
            interval: refreshInterval.value,
        });
    }
};

const stopAutoRefresh = (): void => {
    if (autoRefreshTimer.value) {
        clearInterval(autoRefreshTimer.value);
        autoRefreshTimer.value = null;
        logger.info("Auto-refresh stopped");
    }
};

// Utility methods
const formatLastRefresh = (): string => {
    if (!dashboardStore.lastRefresh) return "";

    const now = new Date();
    const diffMs = now.getTime() - dashboardStore.lastRefresh.getTime();
    const diffMins = Math.floor(diffMs / 60000);

    if (diffMins < 1) return "just now";
    if (diffMins < 60) return `${diffMins}m ago`;

    return dashboardStore.lastRefresh.toLocaleTimeString();
};
</script>

<style scoped>
.project-dashboard {
    display: flex;
    flex-direction: column;
    background: var(--color-white);
    overflow: hidden;
}

/* Header */
.dashboard-header {
    background: var(--color-white);
    border-bottom: 1px solid var(--color-border-light);
    padding: 1rem 1.5rem;
    flex-shrink: 0;
}

.header-content {
    display: flex;
    justify-content: space-between;
    align-items: center;
    max-width: 1500px;
    margin: 0 auto;
}

.header-main {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.dashboard-title {
    margin: 0;
    font-size: 1.5rem;
    font-weight: 700;
    color: var(--color-text-primary);
}

.project-info {
    display: flex;
    flex-direction: column;
    gap: 0.125rem;
}

.project-name {
    font-size: 0.875rem;
    color: var(--color-primary);
    font-weight: 500;
}

.last-updated {
    font-size: 0.75rem;
    color: var(--color-text-secondary);
}

.header-actions {
    display: flex;
    gap: 1rem;
    align-items: center;
}

.header-actions :deep(.btn) {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-size: 0.875rem;
    margin: 0 0.25rem;
}

.dashboard-error :deep(.btn) {
    display: flex;
    align-items: center;
    gap: 0.5rem;
}

/* Content */
.dashboard-content {
    flex: 1;
    overflow-y: auto;
    overflow-x: hidden;
    padding: 1.5rem;
    max-width: 1500px;
    margin: 0 auto;
    width: 100%;
    min-height: 0; /* Allow flex item to shrink */
}

/* Loading State */
.dashboard-loading {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    height: 100%;
    gap: 1rem;
    color: var(--color-text-secondary);
}

.loading-spinner {
    width: 40px;
    height: 40px;
    border: 3px solid var(--color-border-light);
    border-top: 3px solid var(--color-primary);
    border-radius: 50%;
    animation: spin 1s linear infinite;
}

/* Error State */
.dashboard-error {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    height: 100%;
    gap: 1rem;
    text-align: center;
    color: var(--color-error);
}

.dashboard-error i {
    font-size: 3rem;
    opacity: 0.7;
}

.dashboard-error h3 {
    margin: 0;
    font-size: 1.25rem;
    color: var(--color-text-primary);
}

.dashboard-error p {
    margin: 0;
    font-size: 0.875rem;
    color: var(--color-text-secondary);
    max-width: 400px;
}

/* Animations */
@keyframes spin {
    0% {
        transform: rotate(0deg);
    }
    100% {
        transform: rotate(360deg);
    }
}

.rotating {
    animation: spin 1s linear infinite;
}

/* Responsive Design */
@media (max-width: 1200px) {
    .dashboard-content {
        padding: 1rem;
    }
}

@media (max-width: 768px) {
    .dashboard-header {
        padding: 0.75rem 1rem;
    }

    .header-content {
        flex-direction: column;
        gap: 1rem;
        align-items: stretch;
    }

    .header-actions {
        justify-content: center;
        flex-wrap: wrap;
    }

    .dashboard-title {
        font-size: 1.25rem;
    }

    .dashboard-content {
        padding: 0.75rem;
    }

    .header-actions :deep(.btn) {
        font-size: 0.875rem;
    }
}

@media (max-width: 480px) {
    .header-actions {
        grid-template-columns: 1fr 1fr;
        gap: 0.5rem;
    }

    .header-actions :deep(.btn) {
        justify-content: center;
    }
}
</style>
