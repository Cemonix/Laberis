import apiClient from './apiClient';
import type { PaginatedResponse } from '@/types/api/responses';
import type { DataSource, DataSourceType } from '@/types/dataSource';
import type { 
    CreateDataSourceRequest, 
    UpdateDataSourceRequest, 
    DataSourceListParams,
    DataSourceStatsResponse
} from '@/types/dataSource/requests';
import { loggerInstance } from '@/utils/logger';

class DataSourceService {
    private readonly baseUrl = '/projects';
    private readonly log = loggerInstance.createServiceLogger('DataSourceService');

    /**
     * Retrieves all data source types that are configured and available on the server
     * @param projectId The project ID (required for route validation)
     * @returns Promise resolving to array of available data source types
     */
    async getAvailableTypes(projectId: number): Promise<DataSourceType[]> {
        this.log.debug(`Fetching available data source types for project ${projectId}`);
        
        const response = await apiClient.get<DataSourceType[]>(`${this.baseUrl}/${projectId}/datasources/types/available`);
        this.log.info(`Found ${response.data.length} available data source types`);
        return response.data;
    }

    /**
     * Retrieves data sources for a project with optional filtering and pagination
     * @param params Query parameters including projectId and optional filters
     * @returns Promise resolving to paginated data source list
     */
    async getDataSources(params: DataSourceListParams): Promise<PaginatedResponse<DataSource>> {
        this.log.debug(`Fetching data sources for project ${params.projectId}`, params);
        
        const { projectId, ...queryParams } = params;
        const response = await apiClient.get<PaginatedResponse<DataSource>>(`${this.baseUrl}/${projectId}/datasources`, {
            params: queryParams
        });
        this.log.info(`Fetched ${response.data.data?.length || 0} data sources`);
        return response.data;
    }

    /**
     * Retrieves a single data source by its ID
     * @param projectId The unique identifier of the project
     * @param dataSourceId The unique identifier of the data source
     * @returns Promise resolving to the data source
     */
    async getDataSource(projectId: number, dataSourceId: number): Promise<DataSource> {
        this.log.debug(`Fetching data source ${dataSourceId} from project ${projectId}`);
        
        const response = await apiClient.get<DataSource>(`${this.baseUrl}/${projectId}/datasources/${dataSourceId}`);
        this.log.info(`Fetched data source: ${response.data.name}`);
        return response.data;
    }

    /**
     * Creates a new data source within a project
     * @param projectId The unique identifier of the project
     * @param data The data source creation data
     * @returns Promise resolving to the created data source
     */
    async createDataSource(projectId: number, data: CreateDataSourceRequest): Promise<DataSource> {
        this.log.debug(`Creating data source in project ${projectId}`, data);
        
        const response = await apiClient.post<DataSource>(`${this.baseUrl}/${projectId}/datasources`, data);
        this.log.info(`Created data source: ${response.data.name} (ID: ${response.data.id})`);
        return response.data;
    }

    /**
     * Updates an existing data source
     * @param projectId The unique identifier of the project
     * @param dataSourceId The unique identifier of the data source to update
     * @param data The updated data source data
     * @returns Promise resolving to the updated data source
     */
    async updateDataSource(projectId: number, dataSourceId: number, data: UpdateDataSourceRequest): Promise<DataSource> {
        this.log.debug(`Updating data source ${dataSourceId} in project ${projectId}`, data);
        
        const response = await apiClient.put<DataSource>(`${this.baseUrl}/${projectId}/datasources/${dataSourceId}`, data);
        this.log.info(`Updated data source: ${response.data.name}`);
        return response.data;
    }

    /**
     * Permanently deletes a data source and all associated assets
     * @param projectId The unique identifier of the project
     * @param dataSourceId The unique identifier of the data source to delete
     */
    async deleteDataSource(projectId: number, dataSourceId: number): Promise<void> {
        this.log.debug(`Deleting data source ${dataSourceId} from project ${projectId}`);
        
        await apiClient.delete(`${this.baseUrl}/${projectId}/datasources/${dataSourceId}`);
        this.log.info(`Deleted data source ${dataSourceId}`);
    }

    /**
     * Retrieves statistics and metrics for data sources within a project
     * @param projectId The unique identifier of the project
     * @returns Promise resolving to data source statistics
     */
    async getDataSourceStats(projectId: number): Promise<DataSourceStatsResponse> {
        this.log.debug(`Fetching data source stats for project ${projectId}`);
        
        const response = await apiClient.get<DataSourceStatsResponse>(`${this.baseUrl}/${projectId}/datasources/stats`);
        this.log.info(`Fetched stats for project ${projectId}: ${response.data.totalDataSources} total data sources`);
        return response.data;
    }
}

export const dataSourceService = new DataSourceService();
