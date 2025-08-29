import { assetService } from '@/services/project';
import { AssetStatus } from './asset.types';
import type { Asset, AssetLoadResult, AssetValidationResult, WorkspaceAsset } from './asset.types';
import type { ImageDimensions } from '@/core/asset/asset.types';
import { AppLogger } from '@/core/logger/logger';

const logger = AppLogger.createServiceLogger('AssetManager');

/**
 * Asset Manager handles all asset-related operations for the workspace
 * Responsible for loading, validating, and transforming assets
 */
export class AssetManager {
    
    /**
     * Loads an asset by project and asset ID
     * @param projectId The project ID (string)
     * @param assetId The asset ID (string)  
     * @returns Promise resolving to AssetLoadResult
     */
    async loadAsset(projectId: string, assetId: string): Promise<AssetLoadResult> {
        try {
            // Validate input parameters
            const inputValidation = this._validateInputs(projectId, assetId);
            if (!inputValidation.isValid) {
                return {
                    success: false,
                    error: inputValidation.errors[0] || 'Invalid input parameters'
                };
            }

            logger.info(`Loading asset ${assetId} for project ${projectId}`);

            // Convert string IDs to numbers for API calls
            const numericProjectId = parseInt(projectId, 10);
            const numericAssetId = parseInt(assetId, 10);

            if (isNaN(numericProjectId)) {
                return {
                    success: false,
                    error: 'Invalid project ID - must be a valid number'
                };
            }

            if (isNaN(numericAssetId)) {
                return {
                    success: false,
                    error: 'Invalid asset ID - must be a valid number'
                };
            }

            // Load asset from API
            const assetData = await assetService.getAssetById(numericProjectId, numericAssetId);
            
            // Validate the loaded asset
            const validation = this.validateAssetData(assetData);
            if (!validation.isValid) {
                logger.warn(`Loaded asset ${assetId} failed validation:`, validation.errors);
                // Still return success but log warnings - we'll work with what we have
            }

            // Transform asset for workspace use
            const workspaceAsset = this.transformAssetForWorkspace(assetData);

            logger.info(`Successfully loaded asset ${assetId}`, {
                filename: assetData.filename,
                dimensions: workspaceAsset.naturalDimensions,
                hasImageUrl: !!workspaceAsset.imageUrl
            });

            return {
                success: true,
                asset: workspaceAsset.assetData,
                imageUrl: workspaceAsset.imageUrl,
                naturalDimensions: workspaceAsset.naturalDimensions
            };

        } catch (error) {
            const errorMessage = error instanceof Error ? error.message : 'Failed to load asset';
            logger.error(`Failed to load asset ${assetId} for project ${projectId}:`, error);
            
            return {
                success: false,
                error: errorMessage
            };
        }
    }

    /**
     * Validates asset data for completeness and correctness
     * @param asset The asset to validate
     * @returns AssetValidationResult with validation status and any errors
     */
    validateAssetData(asset: Asset): AssetValidationResult {
        const errors: string[] = [];

        // Check if asset exists
        if (!asset) {
            return {
                isValid: false,
                errors: ['Asset is required']
            };
        }

        // Required fields validation
        if (asset.id === undefined || asset.id === null) {
            errors.push('Asset id is required');
        }

        if (!asset.filename || asset.filename.trim() === '') {
            errors.push('Asset filename is required');
        }

        // Optional but should be valid if present
        if (asset.width !== undefined && asset.width <= 0) {
            errors.push('Asset width must be positive');
        }

        if (asset.height !== undefined && asset.height <= 0) {
            errors.push('Asset height must be positive');
        }

        if (asset.sizeBytes !== undefined && asset.sizeBytes < 0) {
            errors.push('Asset sizeBytes cannot be negative');
        }

        // MIME type validation
        if (asset.mimeType && !this._isValidImageMimeType(asset.mimeType)) {
            errors.push('Invalid image MIME type');
        }

        return {
            isValid: errors.length === 0,
            errors
        };
    }

    /**
     * Transforms raw asset data for workspace consumption
     * @param asset The raw asset data
     * @returns WorkspaceAsset with processed data
     */
    transformAssetForWorkspace(asset: Asset): WorkspaceAsset {
        // Extract image URL, defaulting to null if not available
        const imageUrl = asset.imageUrl || null;

        // Create dimensions object if both width and height are available
        let naturalDimensions: ImageDimensions | null = null;
        if (asset.width && asset.height && asset.width > 0 && asset.height > 0) {
            naturalDimensions = {
                width: asset.width,
                height: asset.height
            };
        }

        return {
            assetData: asset,
            imageUrl,
            naturalDimensions
        };
    }

    /**
     * Calculates the aspect ratio of an asset's image
     * @param asset The asset to calculate aspect ratio for
     * @returns The aspect ratio (width/height) or null if dimensions unavailable
     */
    getImageAspectRatio(asset: Asset): number | null {
        if (!asset.width || !asset.height || asset.height === 0) {
            return null;
        }

        return asset.width / asset.height;
    }

    /**
     * Checks if an asset can be loaded in the workspace
     * @param asset The asset to check
     * @returns true if asset can be loaded, false otherwise
     */
    canLoadAsset(asset: Asset): boolean {
        // Only allow loading assets that are ready for annotation
        return asset.status === AssetStatus.READY_FOR_ANNOTATION;
    }

    /**
     * Gets display information for an asset
     * @param asset The asset to get display info for
     * @returns Object with display-friendly information
     */
    getAssetDisplayInfo(asset: Asset) {
        const aspectRatio = this.getImageAspectRatio(asset);
        
        return {
            filename: asset.filename,
            dimensions: asset.width && asset.height 
                ? `${asset.width} × ${asset.height}` 
                : 'Unknown dimensions',
            aspectRatio: aspectRatio ? aspectRatio.toFixed(2) : 'N/A',
            fileSize: asset.sizeBytes 
                ? this._formatFileSize(asset.sizeBytes) 
                : 'Unknown size',
            mimeType: asset.mimeType || 'Unknown type',
            status: asset.status || 'Unknown status'
        };
    }

    /**
     * Validates input parameters for asset loading
     * @private
     */
    private _validateInputs(projectId: string, assetId: string): AssetValidationResult {
        const errors: string[] = [];

        if (!projectId || projectId.trim() === '') {
            errors.push('Project ID is required');
        }

        if (!assetId || assetId.trim() === '') {
            errors.push('Asset ID is required');
        }

        return {
            isValid: errors.length === 0,
            errors
        };
    }

    /**
     * Validates image MIME type
     * @private
     */
    private _isValidImageMimeType(mimeType: string): boolean {
        const validMimeTypes = [
            'image/jpeg',
            'image/jpg', 
            'image/png',
            'image/gif',
            'image/webp',
            'image/bmp',
            'image/svg+xml',
            'image/tiff'
        ];

        return validMimeTypes.includes(mimeType.toLowerCase());
    }

    /**
     * Formats file size in human-readable format
     * @private
     */
    private _formatFileSize(bytes: number): string {
        if (bytes === 0) return '0 Bytes';

        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));

        return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
    }
}