import { ref, computed } from 'vue';
import type { Asset } from '@/types/asset';
import type { Annotation } from '@/types/workspace/annotation';
import type { TaskTableRow } from '@/types/task';
import { assetService, annotationService } from '@/services/api/projects';
import { AppLogger } from '@/utils/logger';
import { 
    AnnotationTypeGuards, 
    AnnotationHelpers,
} from '@/types/workspace/annotationHelpers';

const logger = AppLogger.createComponentLogger('useAssetPreview');

// Shared global state for asset preview functionality
const globalAssetState = {
    loadedAssets: ref<Map<number, Asset>>(new Map()),
    loadingAssets: ref<Set<number>>(new Set()),
    errorAssets: ref<Set<number>>(new Set()),
    assetAnnotations: ref<Map<number, Annotation[]>>(new Map()),
    loadingAnnotations: ref<Set<number>>(new Set()),
    
    // Preview popup state
    showPreviewPopup: ref<boolean>(false),
    previewAsset: ref<TaskTableRow | null>(null),
    previewPopupStyle: ref<Record<string, string>>({}),
    previewImageLoaded: ref<boolean>(false)
};

/**
 * Composable for managing asset preview functionality including:
 * - Asset loading and caching
 * - Annotation loading and display
 * - Loading and error state management
 * - Image preview popups
 * 
 * Uses shared global state so all components see the same data
 */
export function useAssetPreview() {
    // Use shared global state
    const { 
        loadedAssets, loadingAssets, errorAssets, assetAnnotations, loadingAnnotations,
        showPreviewPopup, previewAsset, previewPopupStyle, previewImageLoaded 
    } = globalAssetState;

    // Asset loading functions
    const loadAsset = async (projectId: number, assetId: number) => {
        if (loadingAssets.value.has(assetId) || loadedAssets.value.has(assetId)) {
            return; // Already loading or loaded
        }
        
        loadingAssets.value.add(assetId);
        errorAssets.value.delete(assetId); // Clear any previous error state
        
        try {
            logger.debug(`Loading asset ${assetId} for project ${projectId}`);
            const asset = await assetService.getAssetById(projectId, assetId);
            loadedAssets.value.set(assetId, asset);
            logger.debug(`Successfully loaded asset ${assetId}`, { filename: asset.filename, hasImageUrl: !!asset.imageUrl });
        } catch (error) {
            logger.error(`Failed to load asset ${assetId}`, error);
            errorAssets.value.add(assetId);
            // Set a fallback asset object to prevent repeated loading attempts
            loadedAssets.value.set(assetId, {
                id: assetId,
                filename: `Asset ${assetId}`,
                width: 0,
                height: 0,
                mimeType: 'image/jpeg',
                sizeBytes: 0,
                durationMs: 0,
                status: 'IMPORTED' as any,
                createdAt: new Date().toISOString(),
                updatedAt: new Date().toISOString(),
                imageUrl: undefined
            });
        } finally {
            loadingAssets.value.delete(assetId);
        }
    };

    const loadAnnotations = async (projectId: number, assetId: number) => {
        if (loadingAnnotations.value.has(assetId) || assetAnnotations.value.has(assetId)) {
            return; // Already loading or loaded
        }
        
        loadingAnnotations.value.add(assetId);
        
        try {
            logger.debug(`Loading annotations for asset ${assetId}`);
            const response = await annotationService.getAnnotationsForAsset(projectId, assetId);
            assetAnnotations.value.set(assetId, response.data);
            logger.debug(`Successfully loaded ${response.data.length} annotations for asset ${assetId}`);
        } catch (error) {
            logger.error(`Failed to load annotations for asset ${assetId}`, error);
            // Set empty array to prevent repeated loading attempts
            assetAnnotations.value.set(assetId, []);
        } finally {
            loadingAnnotations.value.delete(assetId);
        }
    };

    // URL generation functions
    const getAssetThumbnailUrl = (projectId: number, assetId: number): string => {
        const asset = loadedAssets.value.get(assetId);
        if (asset?.imageUrl) {
            return asset.imageUrl;
        }
        
        // Load asset if not already loaded or loading
        if (!loadingAssets.value.has(assetId)) {
            loadAsset(projectId, assetId);
        }
        
        // Return empty string - CSS will handle the empty state with background color
        return '';
    };

    const getAssetFullUrl = (projectId: number, assetId?: number): string => {
        if (!assetId) return '';
        
        const asset = loadedAssets.value.get(assetId);
        if (asset?.imageUrl) {
            return asset.imageUrl;
        }
        
        // Load asset if not already loaded or loading  
        if (!loadingAssets.value.has(assetId)) {
            loadAsset(projectId, assetId);
        }
        
        // Return empty string - CSS will handle the empty state
        return '';
    };

    // Annotation helper functions
    const getAnnotationCount = (assetId: number): number => {
        const annotations = assetAnnotations.value.get(assetId);
        return annotations ? annotations.length : 0;
    };

    const getVisibleAnnotations = (assetId: number): Annotation[] => {
        const annotations = assetAnnotations.value.get(assetId) || [];
        // Limit to first 5 annotations to avoid overcrowding the thumbnail
        return annotations.slice(0, 5);
    };

    // Clean annotation type guards using the helper
    const { isBoundingBox, isPoint, isPolygon, isPolyline, isLine, hasPoints } = AnnotationTypeGuards;
    
    // Alias for backward compatibility
    const isBoundingBoxAnnotation = isBoundingBox;
    const isPointAnnotation = isPoint;
    const isPolygonAnnotation = isPolygon;
    const isPolylineAnnotation = isPolyline;
    const isLineAnnotation = isLine;

    // Clean coordinate helper functions using the helper
    const getPolygonPoints = (annotation: Annotation): string => {
        if (hasPoints(annotation)) {
            return AnnotationHelpers.getPointsString(annotation);
        }
        return '';
    };

    const scaleCoordinate = AnnotationHelpers.scaleCoordinate;

    const getScaledPolygonPoints = (annotation: Annotation, assetId: number, targetSize: number): string => {
        if (hasPoints(annotation)) {
            const asset = loadedAssets.value.get(assetId);
            return AnnotationHelpers.getScaledPointsString(annotation, asset?.width, asset?.height, targetSize);
        }
        return '';
    };

    const getAssetDimensions = (assetId: number): { width: number; height: number } => {
        const asset = loadedAssets.value.get(assetId);
        return {
            width: asset?.width || 400,
            height: asset?.height || 300
        };
    };

    // State checking functions
    const hasImageError = (assetId: number): boolean => {
        return errorAssets.value.has(assetId);
    };

    // Preview popup functions
    const showPreview = (event: MouseEvent, task: TaskTableRow) => {
        const rect = (event.target as HTMLElement).getBoundingClientRect();
        previewAsset.value = task;
        
        // Position the popup near the hovered element
        previewPopupStyle.value = {
            left: `${rect.right + 10}px`,
            top: `${rect.top}px`,
            position: 'fixed',
            zIndex: '1000'
        };
        
        showPreviewPopup.value = true;
    };

    const hidePreview = () => {
        showPreviewPopup.value = false;
        previewAsset.value = null;
        previewImageLoaded.value = false;
    };

    // Event handlers
    const handleImageError = (event: Event, tasks: TaskTableRow[]) => {
        const img = event.target as HTMLImageElement;
        // Extract asset ID from the image element or its context
        const assetIdFromSrc = extractAssetIdFromContext(img, tasks);
        if (assetIdFromSrc) {
            errorAssets.value.add(assetIdFromSrc);
            loadingAssets.value.delete(assetIdFromSrc);
        }
        
        // Remove src to trigger CSS styling for empty images
        img.removeAttribute('src');
        // Add error class for styling
        img.classList.add('image-error');
    };

    const handlePreviewImageLoad = () => {
        previewImageLoaded.value = true;
    };

    const handlePreviewImageError = (event: Event) => {
        const img = event.target as HTMLImageElement;
        if (previewAsset.value) {
            errorAssets.value.add(previewAsset.value.assetId);
            loadingAssets.value.delete(previewAsset.value.assetId);
        }
        
        // Remove src to trigger CSS styling for empty images
        img.removeAttribute('src');
        img.classList.add('image-error');
        previewImageLoaded.value = false;
    };

    const extractAssetIdFromContext = (img: HTMLImageElement, tasks: TaskTableRow[]): number | null => {
        // Try to find the asset ID from the closest asset preview cell
        const assetCell = img.closest('.asset-preview-cell');
        if (assetCell) {
            // Look for the asset ID in the thumbnail data
            const thumbnailElement = assetCell.querySelector('.thumbnail-image');
            if (thumbnailElement && thumbnailElement === img) {
                // Find the row data by searching through visible tasks
                // Try to match by the image element's current src or data attributes
                for (const task of tasks) {
                    const asset = loadedAssets.value.get(task.assetId);
                    if (asset?.imageUrl && (img.src === asset.imageUrl || img.getAttribute('src') === asset.imageUrl)) {
                        return task.assetId;
                    }
                }
                
                // Fallback: try to extract from data attributes if they exist
                const assetIdAttr = img.getAttribute('data-asset-id');
                if (assetIdAttr) {
                    const assetId = parseInt(assetIdAttr, 10);
                    if (!isNaN(assetId)) {
                        return assetId;
                    }
                }
            }
        }
        
        // For preview images, check if it's in the preview popup
        if (previewAsset.value && img.closest('.preview-image-container')) {
            return previewAsset.value.assetId;
        }
        
        return null;
    };

    // Preloading function
    const preloadVisibleAssets = (projectId: number, tasks: TaskTableRow[]) => {
        // Preload assets for all visible tasks to improve user experience
        tasks.forEach(task => {
            if (!loadedAssets.value.has(task.assetId) && !loadingAssets.value.has(task.assetId)) {
                // Use a small delay to avoid overwhelming the API
                setTimeout(() => loadAsset(projectId, task.assetId), Math.random() * 1000);
            }
            // Also preload annotations
            if (!assetAnnotations.value.has(task.assetId) && !loadingAnnotations.value.has(task.assetId)) {
                setTimeout(() => loadAnnotations(projectId, task.assetId), Math.random() * 1500);
            }
        });
    };

    // Utility functions
    const formatFileSize = (bytes?: number): string => {
        if (!bytes) return 'Unknown size';
        
        const units = ['B', 'KB', 'MB', 'GB'];
        let size = bytes;
        let unitIndex = 0;
        
        while (size >= 1024 && unitIndex < units.length - 1) {
            size /= 1024;
            unitIndex++;
        }
        
        return `${Math.round(size * 10) / 10} ${units[unitIndex]}`;
    };

    return {
        // State
        loadedAssets: computed(() => loadedAssets.value),
        loadingAssets: computed(() => loadingAssets.value),
        errorAssets: computed(() => errorAssets.value),
        assetAnnotations: computed(() => assetAnnotations.value),
        showPreviewPopup: computed(() => showPreviewPopup.value),
        previewAsset: computed(() => previewAsset.value),
        previewPopupStyle: computed(() => previewPopupStyle.value),
        previewImageLoaded: computed(() => previewImageLoaded.value),
        
        // Functions
        loadAsset,
        loadAnnotations,
        getAssetThumbnailUrl,
        getAssetFullUrl,
        getAnnotationCount,
        getVisibleAnnotations,
        isBoundingBoxAnnotation,
        isPointAnnotation,
        isPolygonAnnotation,
        isPolylineAnnotation,
        isLineAnnotation,
        getPolygonPoints,
        scaleCoordinate,
        getScaledPolygonPoints,
        getAssetDimensions,
        hasImageError,
        showPreview,
        hidePreview,
        handleImageError,
        handlePreviewImageLoad,
        handlePreviewImageError,
        preloadVisibleAssets,
        formatFileSize
    };
}