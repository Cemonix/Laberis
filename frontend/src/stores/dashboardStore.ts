import { defineStore } from "pinia";
import { dashboardService } from "@/services/api/projects/dashboardService";
import type { 
    DashboardState, 
    DashboardLayoutDto,
    WidgetDefinitionDto,
    WidgetInstanceDto,
    WidgetDataRequestDto,
    WidgetDataDto
} from "@/types/dashboard/dashboard";
import { useErrorHandler } from "@/composables/useErrorHandler";
import { AppLogger } from "@/utils/logger";
import { useProjectStore } from "@/stores/projectStore";

const logger = AppLogger.createServiceLogger("DashboardStore");

export const useDashboardStore = defineStore("dashboard", {
    state: (): DashboardState => ({
        loading: false,
        refreshing: false,
        error: null,
        analytics: null,
        configuration: null,
        layout: null,
        widgetDefinitions: [],
        lastRefresh: undefined,
    }),

    getters: {
        isLoaded(state): boolean {
            return !!state.analytics && !!state.layout;
        },
        isConfigured(state): boolean {
            return !!state.configuration;
        },
        visibleWidgets(state): WidgetInstanceDto[] {
            return state.layout?.widgets.filter(widget => widget.isVisible) || [];
        },
        hasData(state): boolean {
            return !!state.analytics;
        },
        refreshIntervalMs(state): number {
            return (state.layout?.refreshIntervalSeconds || 300) * 1000;
        },
        currentTheme(state): string {
            return state.layout?.theme || 'light';
        },
    },

    actions: {
        /**
         * Load dashboard analytics data for the current project
         */
        async loadAnalytics(projectId: number): Promise<void> {
            if (this.loading) return;

            this.loading = true;
            this.error = null;

            try {
                logger.info("Loading dashboard analytics", { projectId });
                const analytics = await dashboardService.getAnalytics(projectId);
                this.analytics = analytics;
                this.lastRefresh = new Date();
                logger.info("Dashboard analytics loaded successfully");
            } catch (error) {
                const { handleError } = useErrorHandler();
                this.error = error instanceof Error ? error.message : "Failed to load dashboard analytics";
                handleError(error, "Dashboard Analytics Loading");
                throw error;
            } finally {
                this.loading = false;
            }
        },

        /**
         * Load dashboard configuration for the current user and project
         */
        async loadConfiguration(projectId: number): Promise<void> {
            try {
                logger.info("Loading dashboard configuration", { projectId });
                const config = await dashboardService.getConfiguration(projectId);
                this.configuration = config;
                
                if (config?.configurationData) {
                    try {
                        const parsedLayout = JSON.parse(config.configurationData) as DashboardLayoutDto;
                        
                        // Ensure we have widgets, if not, use defaults
                        if (parsedLayout.widgets && parsedLayout.widgets.length > 0) {
                            this.layout = parsedLayout;
                        } else {
                            logger.info("No widgets in saved configuration, using defaults");
                            this.layout = this.getDefaultLayout();
                        }
                    } catch (parseError) {
                        logger.warn("Failed to parse dashboard configuration", parseError);
                        this.layout = this.getDefaultLayout();
                    }
                } else {
                    logger.info("No dashboard configuration found, using defaults");
                    this.layout = this.getDefaultLayout();
                }
                
                logger.info("Dashboard configuration loaded successfully", { 
                    widgetCount: this.layout?.widgets?.length || 0 
                });
            } catch (error) {
                logger.warn("Failed to load dashboard configuration, using defaults", error);
                this.layout = this.getDefaultLayout();
            }
        },

        /**
         * Load available widget definitions
         */
        async loadWidgetDefinitions(projectId: number): Promise<void> {
            try {
                logger.info("Loading widget definitions", { projectId });
                const definitions = await dashboardService.getWidgetDefinitions(projectId);
                this.widgetDefinitions = definitions;
                logger.info("Widget definitions loaded successfully");
            } catch (error) {
                logger.error("Failed to load widget definitions", error);
                // Use fallback widget definitions
                this.widgetDefinitions = this.getDefaultWidgetDefinitions();
            }
        },

        /**
         * Save dashboard configuration
         */
        async saveConfiguration(projectId: number): Promise<void> {
            if (!this.layout) {
                throw new Error("No layout to save");
            }

            try {
                logger.info("Saving dashboard configuration", { projectId });
                const configData = JSON.stringify(this.layout);
                
                // Use the unified save configuration method
                const savedConfig = await dashboardService.saveConfiguration(
                    projectId, 
                    { configurationData: configData }
                );
                
                this.configuration = savedConfig;
                this.layout.lastModified = new Date();
                logger.info("Dashboard configuration saved successfully");
            } catch (error) {
                const { handleError } = useErrorHandler();
                this.error = error instanceof Error ? error.message : "Failed to save dashboard configuration";
                handleError(error, "Dashboard Configuration Saving");
                throw error;
            }
        },

        /**
         * Refresh analytics data
         */
        async refreshAnalytics(): Promise<void> {
            const projectStore = useProjectStore();
            const projectId = projectStore.currentProjectId;
            
            if (!projectId) {
                throw new Error("No project selected");
            }

            this.refreshing = true;
            try {
                await this.loadAnalytics(projectId);
            } finally {
                this.refreshing = false;
            }
        },

        /**
         * Initialize dashboard for a project
         */
        async initializeDashboard(projectId: number): Promise<void> {
            this.error = null;
            
            try {
                // Load all dashboard data in parallel
                await Promise.all([
                    this.loadWidgetDefinitions(projectId),
                    this.loadConfiguration(projectId),
                    this.loadAnalytics(projectId)
                ]);
                
                logger.info("Dashboard initialized successfully", { projectId });
            } catch (error) {
                const { handleError } = useErrorHandler();
                this.error = error instanceof Error ? error.message : "Failed to initialize dashboard";
                handleError(error, "Dashboard Initialization");
                throw error;
            }
        },

        /**
         * Add a widget to the dashboard
         */
        addWidget(widgetType: string, title?: string): void {
            if (!this.layout) {
                this.layout = this.getDefaultLayout();
            }

            const definition = this.widgetDefinitions.find(def => def.widgetType === widgetType);
            if (!definition) {
                throw new Error(`Unknown widget type: ${widgetType}`);
            }

            const newWidget: WidgetInstanceDto = {
                widgetId: `widget-${Date.now()}-${Math.random().toString(36).substring(2, 11)}`,
                widgetType,
                title: title || definition.title,
                gridX: 0,
                gridY: 0,
                gridWidth: definition.defaultWidth,
                gridHeight: definition.defaultHeight,
                isVisible: true,
                settings: {},
                lastUpdated: new Date()
            };

            // Find the best position for the new widget
            this.positionWidget(newWidget);
            this.layout.widgets.push(newWidget);
            this.layout.lastModified = new Date();
        },

        /**
         * Remove a widget from the dashboard
         */
        removeWidget(widgetId: string): void {
            if (!this.layout) return;

            const index = this.layout.widgets.findIndex(w => w.widgetId === widgetId);
            if (index >= 0) {
                this.layout.widgets.splice(index, 1);
                this.layout.lastModified = new Date();
            }
        },

        /**
         * Update widget configuration
         */
        updateWidget(widgetId: string, updates: Partial<WidgetInstanceDto>): void {
            if (!this.layout) return;

            const widget = this.layout.widgets.find(w => w.widgetId === widgetId);
            if (widget) {
                Object.assign(widget, updates, { lastUpdated: new Date() });
                this.layout.lastModified = new Date();
            }
        },

        /**
         * Get widget data with filters
         */
        async getWidgetData(request: WidgetDataRequestDto): Promise<WidgetDataDto> {
            const projectStore = useProjectStore();
            const projectId = projectStore.currentProjectId;
            
            if (!projectId) {
                throw new Error("No project selected");
            }

            try {
                logger.info("Loading widget data", { widgetType: request.widgetType });
                return await dashboardService.getWidgetData(projectId, request);
            } catch (error) {
                logger.error("Failed to load widget data", error);
                throw error;
            }
        },

        /**
         * Clear all dashboard state
         */
        clearState(): void {
            this.$reset();
        },

        /**
         * Position a widget in the grid (find the best available spot)
         */
        positionWidget(widget: WidgetInstanceDto): void {
            if (!this.layout) return;

            const gridCols = 12;
            let bestX = 0;
            let bestY = 0;

            // Simple positioning: find the first available spot
            for (let y = 0; y < 100; y++) {
                for (let x = 0; x <= gridCols - widget.gridWidth; x++) {
                    if (this.isPositionAvailable(x, y, widget.gridWidth, widget.gridHeight, widget.widgetId)) {
                        bestX = x;
                        bestY = y;
                        break;
                    }
                }
                if (bestX !== 0 || bestY !== 0) break;
            }

            widget.gridX = bestX;
            widget.gridY = bestY;
        },

        /**
         * Check if a position is available in the grid
         */
        isPositionAvailable(x: number, y: number, width: number, height: number, excludeWidgetId?: string): boolean {
            if (!this.layout) return true;

            for (const widget of this.layout.widgets) {
                if (excludeWidgetId && widget.widgetId === excludeWidgetId) continue;

                const overlapX = x < widget.gridX + widget.gridWidth && x + width > widget.gridX;
                const overlapY = y < widget.gridY + widget.gridHeight && y + height > widget.gridY;

                if (overlapX && overlapY) {
                    return false;
                }
            }

            return true;
        },

        /**
         * Get default dashboard layout with comprehensive widget set
         */
        getDefaultLayout(): DashboardLayoutDto {
            return {
                widgets: [
                    {
                        widgetId: 'project-health',
                        widgetType: 'project_health',
                        title: 'Project Health Overview',
                        gridX: 0,
                        gridY: 0,
                        gridWidth: 6,
                        gridHeight: 5,
                        isVisible: true,
                        settings: {},
                        lastUpdated: new Date()
                    },
                    {
                        widgetId: 'workflow-progress',
                        widgetType: 'workflow_progress',
                        title: 'Workflow Progress',
                        gridX: 6,
                        gridY: 0,
                        gridWidth: 6,
                        gridHeight: 5,
                        isVisible: true,
                        settings: {},
                        lastUpdated: new Date()
                    },
                    {
                        widgetId: 'task-statistics',
                        widgetType: 'task_statistics',
                        title: 'Task Statistics & Trends',
                        gridX: 0,
                        gridY: 5,
                        gridWidth: 8,
                        gridHeight: 4,
                        isVisible: true,
                        settings: {},
                        lastUpdated: new Date()
                    },
                    {
                        widgetId: 'recent-activities',
                        widgetType: 'recent_activities',
                        title: 'Recent Activities',
                        gridX: 8,
                        gridY: 5,
                        gridWidth: 4,
                        gridHeight: 4,
                        isVisible: true,
                        settings: {},
                        lastUpdated: new Date()
                    }
                ],
                theme: 'light',
                refreshIntervalSeconds: 300,
                lastModified: new Date()
            };
        },

        /**
         * Get default widget definitions
         */
        getDefaultWidgetDefinitions(): WidgetDefinitionDto[] {
            return [
                {
                    widgetType: 'project_health',
                    title: 'Project Health',
                    description: 'Overall project status and completion metrics',
                    defaultWidth: 6,
                    defaultHeight: 4,
                    requiresConfiguration: false,
                    availableSettings: [],
                    supportedRoles: ['VIEWER', 'ANNOTATOR', 'REVIEWER', 'MANAGER']
                },
                {
                    widgetType: 'workflow_progress',
                    title: 'Workflow Progress',
                    description: 'Progress through annotation workflow stages',
                    defaultWidth: 6,
                    defaultHeight: 4,
                    requiresConfiguration: false,
                    availableSettings: [],
                    supportedRoles: ['VIEWER', 'ANNOTATOR', 'REVIEWER', 'MANAGER']
                },
                {
                    widgetType: 'user_performance',
                    title: 'User Performance',
                    description: 'Individual user productivity and metrics',
                    defaultWidth: 6,
                    defaultHeight: 4,
                    requiresConfiguration: true,
                    availableSettings: [
                        {
                            key: 'userId',
                            label: 'User',
                            type: 'select',
                            required: false,
                            helpText: 'Select a specific user or leave blank for all users'
                        }
                    ],
                    supportedRoles: ['MANAGER']
                },
                {
                    widgetType: 'task_statistics',
                    title: 'Task Statistics',
                    description: 'Task completion and productivity trends',
                    defaultWidth: 12,
                    defaultHeight: 4,
                    requiresConfiguration: false,
                    availableSettings: [],
                    supportedRoles: ['VIEWER', 'ANNOTATOR', 'REVIEWER', 'MANAGER']
                },
                {
                    widgetType: 'recent_activities',
                    title: 'Recent Activities',
                    description: 'Latest project activities and events',
                    defaultWidth: 6,
                    defaultHeight: 4,
                    requiresConfiguration: false,
                    availableSettings: [],
                    supportedRoles: ['VIEWER', 'ANNOTATOR', 'REVIEWER', 'MANAGER']
                }
            ];
        }
    }
});