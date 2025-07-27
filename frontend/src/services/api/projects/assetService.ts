import { BaseProjectService } from '../base';
import type { PaginatedResponse } from '@/types/api';
import { buildQueryParams } from '@/types/api';
import type { Asset, AssetListParams } from '@/types/asset';
import type { UploadResult, BulkUploadResult } from '@/types/asset';
import { NoFilesProvidedError } from '@/types/asset';
import apiClient from '../apiClient';
import { transformApiError, isValidApiResponse, isValidPaginatedResponse } from '@/services/utils';

/**
 * Service class for managing assets within projects.
 * Note: Upload methods use apiClient directly due to FormData requirements.
 */
class AssetService extends BaseProjectService {
    constructor() {
        super('AssetService');
    }

    /**
     * Uploads a single image file to a project's data source
     */
    async uploadAsset(
        projectId: number, 
        dataSourceId: number, 
        file: File, 
        metadata?: string
    ): Promise<UploadResult> {
        this.logger.info(`Uploading single asset to project ${projectId}, data source ${dataSourceId}`, {
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

            const url = this.buildProjectUrl(projectId, 'assets/upload');
            const response = await apiClient.post<UploadResult>(
                url,
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

            this.logger.info(`Successfully uploaded asset: ${file.name}`, response.data);
            return response.data;
        } catch (error) {
            throw transformApiError(error, `Failed to upload asset: ${file.name}`);
        }
    }

    /**
     * Uploads multiple image files to a project's data source
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
            this.logger.error('Bulk upload attempted with no files');
            throw new NoFilesProvidedError();
        }

        this.logger.info(`Uploading ${files.length} assets to project ${projectId}, data source ${dataSourceId}`, {
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

            const url = this.buildProjectUrl(projectId, 'assets/upload/bulk');
            const response = await apiClient.post<BulkUploadResult>(
                url,
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

            this.logger.info(`Successfully uploaded ${response.data.successfulUploads} of ${files.length} assets`, response.data);
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
     */
    async getAssets(
        projectId: number,
        options: AssetListParams = {}
    ): Promise<PaginatedResponse<Asset>> {
        this.logger.info(`Fetching assets for project ${projectId}`, options);

        try {
            const queryParams = buildQueryParams(options);
            const url = this.buildProjectUrl(projectId, `assets?${queryParams.toString()}`);
            
            const response = await apiClient.get<PaginatedResponse<Asset>>(url);

            if (!isValidPaginatedResponse(response)) {
                throw transformApiError(new Error('Invalid paginated response structure'), 
                    'Failed to fetch assets - Invalid response format');
            }

            this.logger.info(`Fetched ${response.data.data.length} assets for project ${projectId}`, response.data);
            return response.data;
        } catch (error) {
            throw transformApiError(error, 'Failed to fetch assets');
        }
    }

    /**
     * Gets a specific asset by ID
     */
    async getAssetById(projectId: number, assetId: number): Promise<Asset> {
        this.logger.info(`Fetching asset ${assetId} for project ${projectId}`);

        const url = this.buildProjectUrl(projectId, `assets/${assetId}`);
        const response = await this.get<Asset>(url);

        this.logger.info(`Fetched asset ${assetId} for project ${projectId}`, response);
        return response;
    }
}

export const assetService = new AssetService();