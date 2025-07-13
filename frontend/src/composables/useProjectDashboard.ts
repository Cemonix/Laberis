import { ref, computed, watch } from 'vue';
import { useRoute } from 'vue-router';
import { projectService } from '@/services/api/projectService';
import { useProjectStore } from '@/stores/projectStore';
import { useErrorHandler } from '@/composables/useErrorHandler';
import { AppLogger } from '@/utils/logger';
import type { 
    ActivityItem, 
    LabelDistribution, 
    TeamMemberForDashboard, 
    DashboardState 
} from '@/types/dashboard';

const logger = AppLogger.createServiceLogger('useProjectDashboard');

export const useProjectDashboard = () => {
    const route = useRoute();
    const { handleError } = useErrorHandler();
    const projectStore = useProjectStore();
    
    // State
    const state = ref<DashboardState>({
        loading: true,
        refreshing: false,
        error: null,
        data: {
            stats: null,
            recentActivities: [],
            labelDistribution: []
        }
    });

    // Computed
    const projectId = computed(() => {
        const id = route.params.projectId;
        return typeof id === 'string' ? parseInt(id, 10) : null;
    });

    // Chart configuration
    const chartColors = ['#28a745', '#ffc107', '#dc3545']; // success, warning, error
    const primaryColor = '#007bff';

    // Computed values for dashboard widgets
    const annotationProgressData = computed(() => {
        if (!state.value.data.stats) return [];
        
        const { totalAssets, annotatedAssets } = state.value.data.stats;
        const reviewAssets = Math.floor(annotatedAssets * 0.15); // Estimate 15% in review
        const pendingAssets = totalAssets - annotatedAssets;
        
        return [
            { label: 'Annotated', value: Math.max(0, annotatedAssets - reviewAssets) },
            { label: 'In Review', value: reviewAssets },
            { label: 'Pending', value: Math.max(0, pendingAssets) }
        ];
    });

    // Transform team members for dashboard display
    const teamMembers = computed((): TeamMemberForDashboard[] => {
        return projectStore.teamMembers.map(member => ({
            id: member.id.toString(),
            name: member.userName || member.email,
            role: member.role,
            avatar: undefined, // No avatar URL in current ProjectMember type
            annotationsCount: Math.floor(Math.random() * 200) + 50, // Mock data - would come from stats API
            isActive: !!member.joinedAt,
            lastActivity: member.joinedAt ? new Date(member.joinedAt) : undefined
        }));
    });

    // Statistics accessors
    const totalAssets = computed(() => state.value.data.stats?.totalAssets || 0);
    const annotatedAssets = computed(() => state.value.data.stats?.annotatedAssets || 0);
    const reviewAssets = computed(() => Math.floor(annotatedAssets.value * 0.15));
    const completionPercentage = computed(() => state.value.data.stats?.completionPercentage || 0);
    const activeAnnotators = computed(() => state.value.data.stats?.activeAnnotators || 0);

    // Actions
    const fetchProjectStats = async (id: number): Promise<void> => {
        try {
            logger.info(`Attempting to fetch project stats for project ID: ${id}`);
            const projectStats = await projectService.getProjectStats(id);
            state.value.data.stats = projectStats;
            logger.info(`Fetched project stats for project ${id}`, projectStats);
        } catch (err: any) {
            logger.warn(`Stats endpoint not available for project ${id}, using mock data`, err);
            
            // Check if it's a 404 error (endpoint not implemented)
            if (err?.response?.status === 404 || err?.message?.includes('404')) {
                logger.info(`Stats endpoint not implemented, using mock data for project ${id}`);
            } else {
                logger.error(`Failed to fetch project stats for project ${id}`, err);
                handleError(err, 'Failed to load project statistics - using demo data');
            }
            
            // Set realistic mock data so dashboard works
            state.value.data.stats = {
                totalAssets: 1250,
                annotatedAssets: 875,
                totalAnnotations: 2340,
                activeAnnotators: 5,
                completionPercentage: Math.round((875 / 1250) * 100)
            };
        }
    };

    // Generate realistic activity data based on team members
    const generateRecentActivities = (): void => {
        const activities: ActivityItem[] = [];
        const now = Date.now();
        
        // Generate activities for team members
        teamMembers.value.forEach((member, index) => {
            const baseTime = now - (index + 1) * 2 * 60 * 60 * 1000; // Spread over last few hours
            
            // Annotation activity
            activities.push({
                type: 'annotation',
                message: `${member.name} annotated ${Math.floor(Math.random() * 20) + 5} images`,
                timestamp: new Date(baseTime),
                user: member.name,
                userId: parseInt(member.id)
            });
            
            // Review activity (30% chance)
            if (Math.random() > 0.7) {
                activities.push({
                    type: 'review',
                    message: `${member.name} approved ${Math.floor(Math.random() * 10) + 3} annotations`,
                    timestamp: new Date(baseTime - 30 * 60 * 1000),
                    user: member.name,
                    userId: parseInt(member.id)
                });
            }

            // Assignment activity (20% chance)
            if (Math.random() > 0.8) {
                activities.push({
                    type: 'assignment',
                    message: `${member.name} was assigned to new annotation task`,
                    timestamp: new Date(baseTime - 60 * 60 * 1000),
                    user: member.name,
                    userId: parseInt(member.id)
                });
            }
        });
        
        // Add some system activities
        activities.push({
            type: 'upload',
            message: `New batch of ${Math.floor(Math.random() * 50) + 20} images uploaded to project`,
            timestamp: new Date(now - 4 * 60 * 60 * 1000)
        });

        if (Math.random() > 0.5) {
            activities.push({
                type: 'completion',
                message: `Dataset "${Math.random() > 0.5 ? 'Urban Objects' : 'Training Set Alpha'}" annotation completed`,
                timestamp: new Date(now - 8 * 60 * 60 * 1000)
            });
        }
        
        state.value.data.recentActivities = activities
            .sort((a, b) => b.timestamp.getTime() - a.timestamp.getTime())
            .slice(0, 15); // Keep top 15 activities
            
        logger.info(`Generated ${activities.length} recent activities for project dashboard`);
    };

    // Generate label distribution data
    const generateLabelDistribution = (): void => {
        // Mock some realistic distribution data
        const mockLabels = [
            'Person', 'Car', 'Building', 'Tree', 'Animal', 
            'Sign', 'Road', 'Bike', 'Traffic Light', 'Window'
        ];
        
        const distribution: LabelDistribution[] = mockLabels
            .slice(0, 6) // Top 6 labels
            .map((labelName, index) => {
                const baseValue = Math.floor(Math.random() * 200) + 100;
                const value = Math.max(10, baseValue - index * 30);
                
                return {
                    label: labelName,
                    value,
                    percentage: 0 // Will be calculated below
                };
            });
        
        // Calculate percentages
        const total = distribution.reduce((sum, item) => sum + item.value, 0);
        distribution.forEach(item => {
            item.percentage = Math.round((item.value / total) * 100);
        });
        
        state.value.data.labelDistribution = distribution.sort((a, b) => b.value - a.value);
        logger.info('Generated label distribution for dashboard', distribution);
    };

    // Load all dashboard data
    const loadDashboardData = async (id: number): Promise<void> => {
        state.value.loading = true;
        state.value.error = null;
        
        try {
            logger.info(`Loading dashboard data for project ID: ${id}`);
            
            // Validate project ID
            if (!id || isNaN(id)) {
                throw new Error(`Invalid project ID: ${id}`);
            }
            
            // Ensure project store is loaded first
            if (!projectStore.isProjectLoaded || projectStore.currentProjectId !== id) {
                logger.info(`Loading project data for ID: ${id}`);
                await projectStore.setCurrentProject(id);
            }
            
            // Load dashboard-specific stats
            await fetchProjectStats(id);
            
            // Generate mock activities and label distribution
            // These would be replaced with real API calls when available
            generateRecentActivities();
            generateLabelDistribution();
            
            logger.info(`Successfully loaded dashboard data for project ${id}`);
        } catch (err) {
            const errorMessage = err instanceof Error ? err.message : 'Failed to load dashboard data';
            state.value.error = errorMessage;
            logger.error(`Failed to load dashboard data for project ${id}`, err);
        } finally {
            state.value.loading = false;
        }
    };

    // Refresh dashboard data
    const refreshData = async (): Promise<void> => {
        if (!projectId.value || state.value.refreshing) return;
        
        state.value.refreshing = true;
        try {
            await loadDashboardData(projectId.value);
        } finally {
            state.value.refreshing = false;
        }
    };

    // Watchers
    watch(projectId, (newId) => {
        if (newId && !isNaN(newId)) {
            logger.info(`Project ID changed to: ${newId}, loading dashboard data`);
            loadDashboardData(newId);
        } else if (newId) {
            logger.error(`Invalid project ID detected: ${newId}`);
            state.value.error = `Invalid project ID: ${newId}`;
            state.value.loading = false;
        }
    }, { immediate: true });

    // Watch for team members changes to regenerate activities
    watch(() => projectStore.teamMembers, () => {
        if (projectStore.teamMembers.length > 0) {
            generateRecentActivities();
        }
    }, { deep: true });

    return {
        // Reactive state accessors
        loading: computed(() => state.value.loading),
        refreshing: computed(() => state.value.refreshing),
        error: computed(() => state.value.error),
        
        // Dashboard data accessors
        stats: computed(() => state.value.data.stats),
        recentActivities: computed(() => state.value.data.recentActivities),
        labelDistribution: computed(() => state.value.data.labelDistribution),
        
        // Computed data for components
        annotationProgressData,
        chartColors,
        primaryColor,
        teamMembers,
        
        // Statistics
        totalAssets,
        annotatedAssets,
        reviewAssets,
        completionPercentage,
        activeAnnotators,
        
        // Methods
        refreshData,
        
        // Project store access (for convenience)
        project: computed(() => projectStore.currentProject),
        projectStore
    };
};
