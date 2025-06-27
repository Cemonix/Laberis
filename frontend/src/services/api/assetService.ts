import apiClient from './apiClient';
import { transformApiError, isValidApiResponse, isValidPaginatedResponse } from '@/services/utils';
import type { PaginatedResponse } from '@/types/api';
import { buildQueryParams } from '@/types/api';
import type { Asset, AssetListParams } from '@/types/asset';
import type { UploadResult, BulkUploadResult } from '@/types/asset';
import { NoFilesProvidedError } from '@/types/asset';
import { AppLogger } from '@/utils/logger';

const logger = AppLogger.createServiceLogger('AssetService');

class AssetService {
    private readonly baseUrl = '/projects';

    /**
     * Uploads a single image file to a project's data source
     * @param projectId The project ID
     * @param dataSourceId The data source ID
     * @param file The file to upload
     * @param metadata Optional metadata as JSON string
     * @returns Promise resolving to upload result
     */
    async uploadAsset(
        projectId: number, 
        dataSourceId: number, 
        file: File, 
        metadata?: string
    ): Promise<UploadResult> {
        logger.info(`Uploading single asset to project ${projectId}, data source ${dataSourceId}`, {
            filename: file.name,
            size: file.size,
            type: file.type
        });

        try {
            const formData = new FormData();
            formData.append('file', file);
            formData.append('dataSourceId', dataSourceId.toString());
            if (metadata) {
                formData.append('metadata', metadata);
            }

            const response = await apiClient.post<UploadResult>(
                `${this.baseUrl}/${projectId}/assets/upload`,
                formData,
                {
                    headers: {
                        'Content-Type': 'multipart/form-data',
                    },
                }
            );

            if (!isValidApiResponse(response)) {
                throw transformApiError(new Error('Invalid response data'), 
                    'Failed to upload asset - Invalid response format');
            }

            logger.info(`Successfully uploaded asset: ${file.name}`, response.data);
            return response.data;
        } catch (error) {
            throw transformApiError(error, `Failed to upload asset: ${file.name}`);
        }
    }

    /**
     * Uploads multiple image files to a project's data source
     * @param projectId The project ID
     * @param dataSourceId The data source ID
     * @param files The files to upload
     * @param metadata Optional metadata as JSON string
     * @param onProgress Optional progress callback
     * @returns Promise resolving to bulk upload result
     */
    async uploadAssets(
        projectId: number, 
        dataSourceId: number, 
        files: File[], 
        metadata?: string,
        onProgress?: (progress: number) => void
    ): Promise<BulkUploadResult> {
        // Validate that files are provided
        if (!files || files.length === 0) {
            logger.error('Bulk upload attempted with no files');
            throw new NoFilesProvidedError();
        }

        logger.info(`Uploading ${files.length} assets to project ${projectId}, data source ${dataSourceId}`, {
            filenames: files.map(f => f.name),
            totalSize: files.reduce((sum, f) => sum + f.size, 0)
        });

        try {
            const formData = new FormData();
            
            files.forEach(file => {
                formData.append('files', file);
            });
            
            formData.append('dataSourceId', dataSourceId.toString());
            if (metadata) {
                formData.append('metadata', metadata);
            }

            const response = await apiClient.post<BulkUploadResult>(
                `${this.baseUrl}/${projectId}/assets/upload/bulk`,
                formData,
                {
                    headers: {
                        'Content-Type': 'multipart/form-data',
                    },
                    onUploadProgress: (progressEvent) => {
                        if (onProgress && progressEvent.total) {
                            const progress = Math.round((progressEvent.loaded * 100) / progressEvent.total);
                            onProgress(progress);
                        }
                    },
                }
            );

            if (!isValidApiResponse(response)) {
                throw transformApiError(new Error('Invalid response data'), 
                    'Failed to upload assets - Invalid response format');
            }

            logger.info(`Successfully uploaded ${response.data.successfulUploads} of ${files.length} assets`, response.data);
            return response.data;
        } catch (error) {
            if (error instanceof NoFilesProvidedError) {
                throw error;
            }
            throw transformApiError(error, 'Failed to upload assets');
        }
    }

    /**
     * Gets assets for a specific project with optional filtering and pagination
     * @param projectId The project ID
     * @param options Query options for filtering, sorting, and pagination
     * @returns Promise resolving to paginated asset list
     */
    async getAssets(
        projectId: number,
        options: AssetListParams = {}
    ): Promise<PaginatedResponse<Asset>> {
        logger.info(`Fetching assets for project ${projectId}`, options);

        try {
            const queryParams = buildQueryParams(options);

            const response = await apiClient.get<PaginatedResponse<Asset>>(
                `${this.baseUrl}/${projectId}/assets?${queryParams.toString()}`
            );

            if (!isValidPaginatedResponse(response)) {
                throw transformApiError(new Error('Invalid paginated response structure'), 
                    'Failed to fetch assets - Invalid response format');
            }

            logger.info(`Fetched ${response.data.data.length} assets for project ${projectId}`, response.data);
            return response.data;
        } catch (error) {
            throw transformApiError(error, 'Failed to fetch assets');
        }
    }

    /**
     * Gets a specific asset by ID
     * @param projectId The project ID
     * @param assetId The asset ID
     * @returns Promise resolving to the asset
     */
    async getAssetById(projectId: number, assetId: number): Promise<Asset> {
        logger.info(`Fetching asset ${assetId} for project ${projectId}`);

        try {
            const response = await apiClient.get<Asset>(
                `${this.baseUrl}/${projectId}/assets/${assetId}`
            );

            if (!isValidApiResponse(response)) {
                throw transformApiError(new Error('Invalid response data'), 
                    'Failed to fetch asset - Invalid response format');
            }

            logger.info(`Fetched asset ${assetId} for project ${projectId}`, response.data);
            return response.data;
        } catch (error) {
            throw transformApiError(error, `Failed to fetch asset ${assetId}`);
        }
    }

}

export default new AssetService();
