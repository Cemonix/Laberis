import { BaseProjectService } from '../base';
import type { 
    DashboardAnalyticsDto,
    DashboardConfigurationDto,
    CreateUpdateDashboardConfigurationDto,
    WidgetDefinitionDto,
    WidgetDataRequestDto,
    WidgetDataDto
} from '@/types/dashboard/dashboard';

/**
 * Service class for managing dashboard analytics and configuration.
 */
class DashboardService extends BaseProjectService {
    constructor() {
        super('DashboardService');
    }

    /**
     * Get comprehensive dashboard analytics for a project
     */
    async getAnalytics(projectId: number): Promise<DashboardAnalyticsDto> {
        this.logger.info('Fetching dashboard analytics...', { projectId });
        
        const response = await this.get<DashboardAnalyticsDto>(
            this.buildProjectUrl(projectId, 'dashboard/analytics')
        );
        
        this.logger.info('Dashboard analytics fetched successfully', { projectId });
        return response;
    }

    /**
     * Get dashboard configuration for current user and project
     */
    async getConfiguration(projectId: number): Promise<DashboardConfigurationDto | null> {
        this.logger.info('Fetching dashboard configuration...', { projectId });
        
        try {
            const response = await this.get<DashboardConfigurationDto>(
                this.buildProjectUrl(projectId, 'dashboard/configuration')
            );
            
            this.logger.info('Dashboard configuration fetched successfully', { projectId });
            return response;
        } catch (error: any) {
            // Return null if no configuration exists (404)
            if (error.response?.status === 404) {
                this.logger.info('No dashboard configuration found, using defaults', { projectId });
                return null;
            }
            throw error;
        }
    }

    /**
     * Create or update dashboard configuration
     */
    async saveConfiguration(
        projectId: number, 
        request: CreateUpdateDashboardConfigurationDto
    ): Promise<DashboardConfigurationDto> {
        this.logger.info('Saving dashboard configuration...', { projectId });
        
        const response = await this.put<CreateUpdateDashboardConfigurationDto, DashboardConfigurationDto>(
            this.buildProjectUrl(projectId, 'dashboard/configuration'),
            request
        );
        
        this.logger.info('Dashboard configuration saved successfully', { projectId });
        return response;
    }

    /**
     * Get available widget definitions
     */
    async getWidgetDefinitions(projectId: number): Promise<WidgetDefinitionDto[]> {
        this.logger.info('Fetching widget definitions...', { projectId });
        
        const response = await this.get<WidgetDefinitionDto[]>(
            this.buildProjectUrl(projectId, 'dashboard/widgets/definitions')
        );
        
        this.logger.info(`Fetched ${response.length} widget definitions`, { projectId });
        return response;
    }

    /**
     * Get widget data with optional filters
     */
    async getWidgetData(projectId: number, request: WidgetDataRequestDto): Promise<WidgetDataDto> {
        this.logger.info('Fetching widget data...', { projectId, widgetType: request.widgetType });
        
        const response = await this.post<WidgetDataRequestDto, WidgetDataDto>(
            this.buildProjectUrl(projectId, 'dashboard/widgets/data'),
            request
        );
        
        this.logger.info('Widget data fetched successfully', { 
            projectId, 
            widgetType: request.widgetType 
        });
        return response;
    }
}

export const dashboardService = new DashboardService();