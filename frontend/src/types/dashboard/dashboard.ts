import type { ProjectStatsResponse } from '@/types/project/requests';

export interface ActivityItem {
    type: 'annotation' | 'review' | 'assignment' | 'upload' | 'completion';
    message: string;
    timestamp: Date;
    user?: string;
    userId?: number;
}

export interface LabelDistribution {
    label: string;
    value: number;
    percentage: number;
}

export interface TeamMemberForDashboard {
    id: string;
    name: string;
    role: string;
    avatar?: string;
    annotationsCount: number;
    isActive: boolean;
    lastActivity?: Date;
}

export interface DashboardData {
    stats: ProjectStatsResponse | null;
    recentActivities: ActivityItem[];
    labelDistribution: LabelDistribution[];
}

export interface DashboardState {
    loading: boolean;
    refreshing: boolean;
    error: string | null;
    data: DashboardData;
}
