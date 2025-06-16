import apiClient from './apiClient';
import type { PaginatedResponse } from '@/types/api/paginatedResponse';
import type { ApiError } from '@/types/api/error';
import type { DataSource, DataSourceType } from '@/types/dataSource/dataSource';
import type { 
    CreateDataSourceRequest, 
    UpdateDataSourceRequest, 
    DataSourceListParams,
    DataSourceStatsResponse
} from '@/types/dataSource/requests';
import { AppLogger } from '@/utils/logger';

const logger = AppLogger.createServiceLogger('DataSourceService');

class DataSourceService {
    private readonly baseUrl = '/projects';

    /**
     * Retrieves all data source types that are configured and available on the server
     * @param projectId The project ID
     * @returns Promise resolving to array of available data source types
     */
    async getAvailableTypes(projectId: number): Promise<DataSourceType[]> {
        logger.info(`Fetching available data source types for project ${projectId}`);
        try {
            const response = await apiClient.get<DataSourceType[]>(`${this.baseUrl}/${projectId}/datasources/types/available`);
            if (response && response.data) {
                logger.info(`Found ${response.data.length} available data source types for project ${projectId}.`, response.data);
                return response.data;
            } else {
                logger.error(`Invalid response structure for available types for project ${projectId}.`, response);
                throw new Error('Invalid response structure from API for available data source types.');
            }
        } catch (error) {
            const apiError = error as ApiError;
            logger.error(`Failed to fetch available data source types for project ${projectId}.`, apiError.response?.data || apiError.message);
            throw apiError;
        }
    }

    /**
     * Retrieves data sources for a project with optional filtering and pagination
     * @param params Query parameters including projectId and optional filters
     * @returns Promise resolving to paginated data source list
     */
    async getDataSources(params: DataSourceListParams): Promise<PaginatedResponse<DataSource>> {
        logger.info(`Fetching data sources for project ${params.projectId}`, params);
        try {
            const { projectId, ...queryParams } = params;
            const response = await apiClient.get<PaginatedResponse<DataSource>>(`${this.baseUrl}/${projectId}/datasources`, {
                params: queryParams
            });

            if (response && response.data && Array.isArray(response.data.data)) {
                logger.info(`Fetched ${response.data.data.length} data sources (total: ${response.data.totalItems}) for project ${projectId}.`, response.data);
                return response.data;
            } else {
                logger.error(`Invalid response structure for data sources for project ${projectId}.`, response);
                throw new Error('Invalid response structure from API for data sources.');
            }
        } catch (error) {
            const apiError = error as ApiError;
            logger.error(`Failed to fetch data sources for project ${params.projectId}.`, apiError.response?.data || apiError.message);
            throw apiError;
        }
    }

    /**
     * Retrieves a single data source by its ID
     * @param projectId The unique identifier of the project
     * @param dataSourceId The unique identifier of the data source
     * @returns Promise resolving to the data source
     */
    async getDataSource(projectId: number, dataSourceId: number): Promise<DataSource> {
        logger.info(`Fetching data source ${dataSourceId} from project ${projectId}`);
        try {
            const response = await apiClient.get<DataSource>(`${this.baseUrl}/${projectId}/datasources/${dataSourceId}`);
            if (response && response.data && response.data.id) {
                logger.info(`Fetched data source: ${response.data.name} (ID: ${response.data.id}) from project ${projectId}.`, response.data);
                return response.data;
            } else {
                logger.error(`Invalid response structure for data source ${dataSourceId} from project ${projectId}.`, response);
                throw new Error('Invalid response structure from API for single data source.');
            }
        } catch (error) {
            const apiError = error as ApiError;
            logger.error(`Failed to fetch data source ${dataSourceId} from project ${projectId}.`, apiError.response?.data || apiError.message);
            throw apiError;
        }
    }

    /**
     * Creates a new data source within a project
     * @param projectId The unique identifier of the project
     * @param data The data source creation data
     * @returns Promise resolving to the created data source
     */
    async createDataSource(projectId: number, data: CreateDataSourceRequest): Promise<DataSource> {
        logger.info(`Creating data source in project ${projectId}`, data);
        try {
            const response = await apiClient.post<DataSource>(`${this.baseUrl}/${projectId}/datasources`, data);
            if (response && response.data && response.data.id) {
                logger.info(`Created data source: ${response.data.name} (ID: ${response.data.id}) in project ${projectId}.`, response.data);
                return response.data;
            } else {
                logger.error(`Invalid response structure after creating data source in project ${projectId}.`, response);
                throw new Error('Invalid response structure from API after creating data source.');
            }
        } catch (error) {
            const apiError = error as ApiError;
            logger.error(`Failed to create data source in project ${projectId}.`, apiError.response?.data || apiError.message);
            throw apiError;
        }
    }

    /**
     * Updates an existing data source
     * @param projectId The unique identifier of the project
     * @param dataSourceId The unique identifier of the data source to update
     * @param data The updated data source data
     * @returns Promise resolving to the updated data source
     */
    async updateDataSource(projectId: number, dataSourceId: number, data: UpdateDataSourceRequest): Promise<DataSource> {
        logger.info(`Updating data source ${dataSourceId} in project ${projectId}`, data);
        try {
            const response = await apiClient.put<DataSource>(`${this.baseUrl}/${projectId}/datasources/${dataSourceId}`, data);
            if (response && response.data && response.data.id) {
                logger.info(`Updated data source: ${response.data.name} (ID: ${response.data.id}) in project ${projectId}.`, response.data);
                return response.data;
            } else {
                logger.error(`Invalid response structure after updating data source ${dataSourceId} in project ${projectId}.`, response);
                throw new Error('Invalid response structure from API after updating data source.');
            }
        } catch (error) {
            const apiError = error as ApiError;
            logger.error(`Failed to update data source ${dataSourceId} in project ${projectId}.`, apiError.response?.data || apiError.message);
            throw apiError;
        }
    }

    /**
     * Permanently deletes a data source and all associated assets
     * @param projectId The unique identifier of the project
     * @param dataSourceId The unique identifier of the data source to delete
     */
    async deleteDataSource(projectId: number, dataSourceId: number): Promise<void> {
        logger.info(`Deleting data source ${dataSourceId} from project ${projectId}`);
        try {
            const response = await apiClient.delete(`${this.baseUrl}/${projectId}/datasources/${dataSourceId}`);
            if (response.status === 204 || response.status === 200) {
                logger.info(`Deleted data source ${dataSourceId} from project ${projectId} successfully.`);
            } else {
                logger.error(`Unexpected status code after deleting data source ${dataSourceId} from project ${projectId}.`, response);
                throw new Error('Unexpected response from API after deleting data source.');
            }
        } catch (error) {
            const apiError = error as ApiError;
            logger.error(`Failed to delete data source ${dataSourceId} from project ${projectId}.`, apiError.response?.data || apiError.message);
            throw apiError;
        }
    }

    /**
     * Retrieves statistics and metrics for data sources within a project
     * @param projectId The unique identifier of the project
     * @returns Promise resolving to data source statistics
     */
    async getDataSourceStats(projectId: number): Promise<DataSourceStatsResponse> {
        logger.info(`Fetching data source stats for project ${projectId}`);
        try {
            const response = await apiClient.get<DataSourceStatsResponse>(`${this.baseUrl}/${projectId}/datasources/stats`);
            if (response && response.data) { // Add more specific checks if needed based on DataSourceStatsResponse structure
                logger.info(`Fetched stats for project ${projectId}: ${response.data.totalDataSources} total data sources.`, response.data);
                return response.data;
            } else {
                logger.error(`Invalid response structure for data source stats for project ${projectId}.`, response);
                throw new Error('Invalid response structure from API for data source stats.');
            }
        } catch (error) {
            const apiError = error as ApiError;
            logger.error(`Failed to fetch data source stats for project ${projectId}.`, apiError.response?.data || apiError.message);
            throw apiError;
        }
    }
}

export const dataSourceService = new DataSourceService();
