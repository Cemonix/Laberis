import apiClient from './apiClient';
import type { ApiError } from '@/types/api/error';
import type { UploadResult, BulkUploadResult } from '@/types/asset';
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
            );

            if (response && response.data) {
                logger.info(`Successfully uploaded asset: ${file.name}`, response.data);
                return response.data;
            } else {
                logger.error(`Invalid response structure for asset upload`, response);
                throw new Error('Invalid response structure from API for asset upload.');
            }
        } catch (error) {
            const apiError = error as ApiError;
            logger.error(`Failed to upload asset: ${file.name}`, apiError.response?.data || apiError.message);
            throw apiError;
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
                throw new Error('Invalid response structure from API for bulk asset upload.');
            }
        } catch (error) {
            const apiError = error as ApiError;
            logger.error(`Failed to upload assets`, apiError.response?.data || apiError.message);
            throw apiError;
        }
    }
}

export default new AssetService();
