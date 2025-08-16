/**
 * TypeScript interfaces for Dashboard Analytics DTOs
 * These interfaces match the backend C# DTOs for consistent API communication
 */

// Core Analytics DTOs

export interface WorkflowStageProgressDto {
    workflowStageId: number;
    stageName: string;
    stageOrder: number;
    stageType: string;
    totalTasks: number;
    completedTasks: number;
    inProgressTasks: number;
    averageCompletionTimeHours: number;
    lastActivityAt?: Date;
}

export interface WorkflowProgressDto {
    workflowId: number;
    workflowName: string;
    totalTasks: number;
    completedTasks: number;
    inProgressTasks: number;
    notStartedTasks: number;
    suspendedTasks: number;
    deferredTasks: number;
    completionPercentage: number;
    stageProgress: WorkflowStageProgressDto[];
}

export interface DailyProductivityDto {
    date: Date;
    tasksCompleted: number;
    hoursWorked: number;
}

export interface UserPerformanceDto {
    userId: string;
    userName: string;
    userEmail: string;
    role: string;
    totalTasksAssigned: number;
    tasksCompleted: number;
    tasksInProgress: number;
    tasksVetoed: number;
    completionRate: number;
    vetoRate: number;
    averageTaskCompletionTimeHours: number;
    tasksPerDay: number;
    lastActivityAt?: Date;
    dailyProductivity: DailyProductivityDto[];
}

export interface RecentActivityDto {
    activityType: string;
    description: string;
    userId: string;
    userName: string;
    timestamp: Date;
    assetName?: string;
    workflowName?: string;
    stageName?: string;
}

export interface ProjectHealthDto {
    projectId: number;
    projectName: string;
    overallCompletionPercentage: number;
    qualityScore: number;
    productivityScore: number;
    totalMembers: number;
    activeMembers: number;
    totalAssets: number;
    completedAssets: number;
    bottlenecks: string[];
    lastActivityAt: Date;
}

export interface TaskStatusDistributionDto {
    status: string;
    count: number;
    percentage: number;
}

export interface DailyTaskCountDto {
    date: Date;
    tasksCreated: number;
    tasksCompleted: number;
    tasksVetoed: number;
}

export interface TaskStatisticsDto {
    dateRange: string;
    totalTasks: number;
    completedTasks: number;
    vetoedTasks: number;
    averageCompletionTimeHours: number;
    productivityTrend: number;
    statusDistribution: TaskStatusDistributionDto[];
    dailyTaskCounts: DailyTaskCountDto[];
}

export interface DashboardAnalyticsDto {
    projectHealth: ProjectHealthDto;
    workflowProgress: WorkflowProgressDto[];
    userPerformance: UserPerformanceDto[];
    recentActivities: RecentActivityDto[];
    taskStatistics: TaskStatisticsDto;
    generatedAt: Date;
}

// Widget System DTOs

export interface WidgetSettingOptionDto {
    value: string;
    label: string;
}

export interface WidgetSettingDto {
    key: string;
    label: string;
    type: string;
    defaultValue?: any;
    options?: WidgetSettingOptionDto[];
    required: boolean;
    validationRegex?: string;
    helpText?: string;
}

export interface WidgetDefinitionDto {
    widgetType: string;
    title: string;
    description: string;
    defaultWidth: number;
    defaultHeight: number;
    requiresConfiguration: boolean;
    availableSettings: WidgetSettingDto[];
    supportedRoles: string[];
}

export interface WidgetInstanceDto {
    widgetId: string;
    widgetType: string;
    title: string;
    gridX: number;
    gridY: number;
    gridWidth: number;
    gridHeight: number;
    isVisible: boolean;
    settings: Record<string, any>;
    lastUpdated: Date;
}

export interface DashboardLayoutDto {
    widgets: WidgetInstanceDto[];
    theme: string;
    refreshIntervalSeconds: number;
    lastModified: Date;
}

export interface DashboardConfigurationDto {
    dashboardConfigurationId: number;
    configurationData: string;
    createdAt: Date;
    updatedAt: Date;
    userId: string;
    projectId: number;
}

export interface CreateUpdateDashboardConfigurationDto {
    configurationData: string;
}

export interface WidgetDataRequestDto {
    widgetType: string;
    workflowId?: number;
    workflowStageId?: number;
    userId?: string;
    dateFrom?: Date;
    dateTo?: Date;
    limit?: number;
    customFilters: Record<string, any>;
}

export interface WidgetDataDto {
    widgetType: string;
    data: any;
    generatedAt: Date;
    expiresAt?: Date;
    metadata: Record<string, any>;
}

// Frontend State Management Types

export interface DashboardState {
    loading: boolean;
    refreshing: boolean;
    error: string | null;
    analytics: DashboardAnalyticsDto | null;
    configuration: DashboardConfigurationDto | null;
    layout: DashboardLayoutDto | null;
    widgetDefinitions: WidgetDefinitionDto[];
    lastRefresh?: Date;
}

export interface WidgetFilter {
    dateFrom?: Date;
    dateTo?: Date;
    workflowId?: number;
    userId?: string;
}

// Chart Data Interfaces

export interface ChartDataPoint {
    label: string;
    value: number;
    color?: string;
}

export interface TimeSeriesDataPoint {
    date: Date;
    value: number;
    label?: string;
}

export interface ProgressChartData {
    total: number;
    completed: number;
    inProgress: number;
    notStarted: number;
    suspended: number;
    deferred: number;
}

