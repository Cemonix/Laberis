import { BaseProjectService } from '../base';
import type { PaginatedResponse } from '@/types/api/paginatedResponse';
import type { DataSource, DataSourceType } from '@/types/dataSource/dataSource';
import type { 
    CreateDataSourceRequest, 
    UpdateDataSourceRequest, 
    DataSourceListParams,
    DataSourceStatsResponse
} from '@/types/dataSource/requests';

/**
 * Service class for managing data sources within projects.
 * Extends BaseProjectService to inherit common project-related functionality.
 */
class DataSourceService extends BaseProjectService {
    constructor() {
        super('DataSourceService');
    }

    /**
     * Retrieves all data source types that are configured and available on the server
     */
    async getAvailableTypes(projectId: number): Promise<DataSourceType[]> {
        this.logger.info(`Fetching available data source types for project ${projectId}`);
        
        const url = this.buildProjectUrl(projectId, 'datasources/types/available');
        const response = await this.get<DataSourceType[]>(url);
        
        this.logger.info(`Found ${response.length} available data source types for project ${projectId}.`, response);
        return response;
    }

    /**
     * Retrieves data sources for a project with optional filtering and pagination
     */
    async getDataSources(params: DataSourceListParams): Promise<PaginatedResponse<DataSource>> {
        this.logger.info(`Fetching data sources for project ${params.projectId}`, params);
        
        const { projectId, ...queryParams } = params;
        
        if (!projectId) {
            throw new Error('Project ID is required for fetching data sources');
        }
        
        const url = this.buildProjectUrl(projectId, 'datasources');
        const response = await this.getPaginated<DataSource>(url, queryParams);

        this.logger.info(`Fetched ${response.data.length} data sources (total: ${response.totalItems}) for project ${projectId}.`, response);
        return response;
    }

    /**
     * Retrieves a single data source by its ID
     */
    async getDataSource(projectId: number, dataSourceId: number): Promise<DataSource> {
        this.logger.info(`Fetching data source ${dataSourceId} from project ${projectId}`);
        
        const url = this.buildProjectUrl(projectId, `datasources/${dataSourceId}`);
        const response = await this.get<DataSource>(url);
        
        this.logger.info(`Fetched data source: ${response.name} (ID: ${response.id}) from project ${projectId}.`, response);
        return response;
    }

    /**
     * Creates a new data source within a project
     */
    async createDataSource(projectId: number, data: CreateDataSourceRequest): Promise<DataSource> {
        this.logger.info(`Creating data source in project ${projectId}`, data);
        
        const url = this.buildProjectUrl(projectId, 'datasources');
        const response = await this.post<CreateDataSourceRequest, DataSource>(url, data);
        
        this.logger.info(`Created data source: ${response.name} (ID: ${response.id}) in project ${projectId}.`, response);
        return response;
    }

    /**
     * Updates an existing data source
     */
    async updateDataSource(projectId: number, dataSourceId: number, data: UpdateDataSourceRequest): Promise<DataSource> {
        this.logger.info(`Updating data source ${dataSourceId} in project ${projectId}`, data);
        
        const url = this.buildProjectUrl(projectId, `datasources/${dataSourceId}`);
        const response = await this.put<UpdateDataSourceRequest, DataSource>(url, data);
        
        this.logger.info(`Updated data source: ${response.name} (ID: ${response.id}) in project ${projectId}.`, response);
        return response;
    }

    /**
     * Permanently deletes a data source and all associated assets
     */
    async deleteDataSource(projectId: number, dataSourceId: number): Promise<void> {
        this.logger.info(`Deleting data source ${dataSourceId} from project ${projectId}`);
        
        const url = this.buildProjectUrl(projectId, `datasources/${dataSourceId}`);
        await this.delete(url);
        
        this.logger.info(`Deleted data source ${dataSourceId} from project ${projectId} successfully.`);
    }

    /**
     * Retrieves statistics and metrics for data sources within a project
     */
    async getDataSourceStats(projectId: number): Promise<DataSourceStatsResponse> {
        this.logger.info(`Fetching data source stats for project ${projectId}`);
        
        const url = this.buildProjectUrl(projectId, 'datasources/stats');
        const response = await this.get<DataSourceStatsResponse>(url);
        
        this.logger.info(`Fetched stats for project ${projectId}: ${response.totalDataSources} total data sources.`, response);
        return response;
    }
}

export const dataSourceService = new DataSourceService();