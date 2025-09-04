import { BaseProjectService } from '../baseProjectService';
import { buildQueryParams } from '@/services/base/requests';
import type { AssetListParams } from '@/services/project/asset';
import type { UploadResult, BulkUploadResult } from '@/services/project/asset';
import { NoFilesProvidedError } from '@/services/project/asset';
import apiClient from '../../apiClient';
import { transformApiError, isValidApiResponse, isValidPaginatedResponse } from '@/services/interceptors';
import type { PaginatedResponse } from '@/services/base/paginatedResponse';
import type { Asset } from '@/core/asset/asset.types';

/**
 * Service class for managing assets within projects.
 * Note: Upload methods use apiClient directly due to FormData requirements.
 */
class AssetService extends BaseProjectService {
    private static readonly MAX_CHUNK_SIZE = 25 * 1024 * 1024; // 25MB

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

        const totalSize = files.reduce((sum, f) => sum + f.size, 0);
        this.logger.info(`Uploading ${files.length} assets to project ${projectId}, data source ${dataSourceId}`, {
            filenames: files.map(f => f.name),
            totalSize
        });

        // Check if we need to use chunked upload (25MB limit with some buffer)
        const needsChunking = totalSize > AssetService.MAX_CHUNK_SIZE || files.length > 15;

        if (needsChunking) {
            return this.uploadAssetsInChunks(projectId, dataSourceId, files, metadata, onProgress);
        }

        // Use direct upload for smaller batches
        return this.uploadAssetsBatch(projectId, dataSourceId, files, metadata, onProgress);
    }

    /**
     * Uploads assets in chunks to avoid request size limits
     */
    private async uploadAssetsInChunks(
        projectId: number, 
        dataSourceId: number, 
        files: File[], 
        metadata?: string,
        onProgress?: (progress: number) => void
    ): Promise<BulkUploadResult> {
        const chunks: File[][] = [];
        let currentChunk: File[] = [];
        let currentChunkSize = 0;

        // Split files into chunks based on size
        for (const file of files) {
            // If adding this file would exceed the chunk size, start a new chunk
            if (currentChunkSize + file.size > AssetService.MAX_CHUNK_SIZE && currentChunk.length > 0) {
                chunks.push(currentChunk);
                currentChunk = [file];
                currentChunkSize = file.size;
            } else {
                currentChunk.push(file);
                currentChunkSize += file.size;
            }
        }

        // Add the last chunk if it has files
        if (currentChunk.length > 0) {
            chunks.push(currentChunk);
        }

        this.logger.info(`Splitting upload into ${chunks.length} chunks`, {
            chunkSizes: chunks.map(chunk => chunk.reduce((sum, f) => sum + f.size, 0))
        });

        // Upload chunks sequentially
        const results: BulkUploadResult[] = [];
        let totalProcessed = 0;

        for (let i = 0; i < chunks.length; i++) {
            const chunk = chunks[i];
            const chunkResult = await this.uploadAssetsBatch(
                projectId,
                dataSourceId,
                chunk,
                metadata,
                (chunkProgress) => {
                    // Calculate overall progress
                    const baseProgress = (totalProcessed / files.length) * 100;
                    const chunkProgressContribution = (chunk.length / files.length) * (chunkProgress / 100) * 100;
                    const overallProgress = Math.round(baseProgress + chunkProgressContribution);
                    onProgress?.(Math.min(overallProgress, 100));
                }
            );

            results.push(chunkResult);
            totalProcessed += chunk.length;

            this.logger.info(`Completed chunk ${i + 1}/${chunks.length}`, {
                chunkFiles: chunk.length,
                chunkSuccesses: chunkResult.successfulUploads,
                chunkFailures: chunkResult.failedUploads
            });
        }

        // Combine results
        const successfulUploads = results.reduce((sum, r) => sum + r.successfulUploads, 0);
        const failedUploads = results.reduce((sum, r) => sum + r.failedUploads, 0);
        const combinedResult: BulkUploadResult = {
            totalFiles: files.length,
            successfulUploads,
            failedUploads,
            results: results.flatMap(r => r.results),
            allSuccessful: results.every(r => r.allSuccessful),
            summary: `${successfulUploads} of ${files.length} files uploaded successfully${failedUploads > 0 ? ` (${failedUploads} failed)` : ''}`
        };

        this.logger.info(`Chunked upload completed: ${combinedResult.successfulUploads} successes, ${combinedResult.failedUploads} failures`);
        return combinedResult;
    }

    /**
     * Uploads a batch of assets (internal method)
     */
    private async uploadAssetsBatch(
        projectId: number, 
        dataSourceId: number, 
        files: File[], 
        metadata?: string,
        onProgress?: (progress: number) => void
    ): Promise<BulkUploadResult> {
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

    /**
     * Get the count of available assets for a project
     */
    async getAvailableAssetsCount(projectId: number): Promise<number> {
        this.logger.info(`Checking available assets for project ${projectId}`);

        try {
            const url = this.buildProjectUrl(projectId, `assets/available-assets-count`);
            const data = await this.get<{ count: number }>(url);

            this.logger.info(`Project ${projectId} has ${data.count} assets available`);

            return data.count;
        } catch (error) {
            this.logger.warn('Failed to check assets availability:', error);
            return 0;
        }
    }

    /**
     * Gets the count of available assets for a data source in a project
     */
    async getAvailableAssetsCountForDataSource(projectId: number, dataSourceId: number): Promise<number> {
        this.logger.info(`Checking available assets for project ${projectId} in data source ${dataSourceId}`);

        try {
            const url = this.buildProjectUrl(projectId, `assets/available-assets-for-data-source-count?dataSourceId=${dataSourceId}`);
            const data = await this.get<{ count: number }>(url);

            const count = data.count;

            this.logger.info(`Project ${projectId} has ${count} assets available in data source ${dataSourceId}`);

            return count;
        } catch (error) {
            this.logger.warn('Failed to check assets availability:', error);
            return 0;
        }
    }
}

export const assetService = new AssetService();