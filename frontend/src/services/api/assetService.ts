import apiClient from './apiClient';
import type { ApiError, PaginatedResponse } from '@/types/api';
import { buildQueryParams } from '@/types/api';
import type { Asset, AssetListParams } from '@/types/asset';
import type { UploadResult, BulkUploadResult } from '@/types/asset';
import {  NoFilesProvidedError } from '@/types/asset';
import { ApiResponseError, ServerError, NetworkError } from '@/types/common/errors';
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

            if (response && response.data) {
                logger.info(`Successfully uploaded asset: ${file.name}`, response.data);
                return response.data;
            } else {
                logger.error(`Invalid response structure for asset upload`, response);
                throw new ApiResponseError('Invalid response structure from API for asset upload.');
            }
        } catch (error) {
            if (error instanceof ApiResponseError) {
                throw error;
            }

            const apiError = error as ApiError;
            logger.error(`Failed to upload asset: ${file.name}`, apiError.response?.data || apiError.message);
            
            if (apiError.response) {
                throw new ServerError(
                    apiError.response.data?.message || 'Server error occurred',
                    apiError.response.status,
                    apiError.response.data
                );
            }
            
            if (apiError.code === 'NETWORK_ERROR' || apiError.request) {
                throw new NetworkError(apiError.message, apiError);
            }

            throw new NetworkError(apiError.message || 'Unknown upload error', apiError);
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

            if (response && response.data) {
                logger.info(`Successfully uploaded ${response.data.successfulUploads} of ${files.length} assets`, response.data);
                return response.data;
            } else {
                logger.error(`Invalid response structure for bulk asset upload`, response);
                throw new ApiResponseError('Invalid response structure from API for bulk asset upload.');
            }
        } catch (error) {
            if (error instanceof NoFilesProvidedError || error instanceof ApiResponseError) {
                throw error;
            }

            const apiError = error as ApiError;
            logger.error(`Failed to upload assets`, apiError.response?.data || apiError.message);
            
            if (apiError.response) {
                throw new ServerError(
                    apiError.response.data?.message || 'Server error occurred during bulk upload',
                    apiError.response.status,
                    apiError.response.data
                );
            }
            
            if (apiError.code === 'NETWORK_ERROR' || apiError.request) {
                throw new NetworkError(apiError.message, apiError);
            }

            throw new NetworkError(apiError.message || 'Unknown bulk upload error', apiError);
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

            if (response && response.data && Array.isArray(response.data.data)) {
                logger.info(`Fetched ${response.data.data.length} assets for project ${projectId}`, response.data);
                return response.data;
            } else {
                logger.error(`Invalid response structure for assets for project ${projectId}`, response);
                throw new ApiResponseError('Invalid response structure from API for assets.');
            }
        } catch (error) {
            const apiError = error as ApiError;
            logger.error(`Failed to fetch assets for project ${projectId}`, apiError.response?.data || apiError.message);
            
            if (apiError.response) {
                throw new ServerError(
                    apiError.response.data?.message || 'Server error occurred while fetching assets',
                    apiError.response.status,
                    apiError.response.data
                );
            }
            
            if (apiError.code === 'NETWORK_ERROR' || apiError.request) {
                throw new NetworkError(apiError.message, apiError);
            }

            throw new NetworkError(apiError.message || 'Unknown error while fetching assets', apiError);
        }
    }

}

export default new AssetService();
