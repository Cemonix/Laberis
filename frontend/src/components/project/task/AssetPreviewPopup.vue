<template>
    <div 
        v-if="visible && previewAsset" 
        class="asset-preview-popup"
        :style="popupStyle"
    >
        <div class="preview-header">
            <span class="preview-asset-name">{{ previewAsset.assetName }}</span>
            <div v-if="previewAsset && loadedAssets.get(previewAsset.assetId)" class="asset-metadata">
                <span class="asset-size">{{ formatFileSize(loadedAssets.get(previewAsset.assetId)?.sizeBytes) }}</span>
                <span class="asset-dimensions" v-if="loadedAssets.get(previewAsset.assetId)?.width && loadedAssets.get(previewAsset.assetId)?.height">
                    {{ loadedAssets.get(previewAsset.assetId)?.width }}×{{ loadedAssets.get(previewAsset.assetId)?.height }}
                </span>
                <span v-if="getAnnotationCount(previewAsset.assetId) > 0" class="annotation-info">
                    <font-awesome-icon :icon="faShapes" />
                    {{ getAnnotationCount(previewAsset.assetId) }} annotation{{ getAnnotationCount(previewAsset.assetId) === 1 ? '' : 's' }}
                </span>
            </div>
        </div>
        <div class="preview-image-container">
            <div v-if="previewAsset && loadingAssets.has(previewAsset.assetId)" class="loading-state">
                <font-awesome-icon :icon="faRefresh" spin class="loading-icon-large" />
                <span>Loading preview...</span>
            </div>
            <div v-else-if="previewAsset && hasImageError(previewAsset.assetId)" class="error-state">
                <font-awesome-icon :icon="faExclamationTriangle" class="error-icon-large" />
                <span>Image failed to load</span>
            </div>
            <div v-else class="preview-image-wrapper">
                <img 
                    :src="getAssetFullUrl(projectId, previewAsset?.assetId)" 
                    :alt="previewAsset?.assetName"
                    class="preview-image"
                    @error="onPreviewImageError"
                    @load="onPreviewImageLoad"
                />
                <!-- Annotation overlay for preview -->
                <svg 
                    v-if="previewAsset && getAnnotationCount(previewAsset.assetId) > 0 && previewImageLoaded" 
                    class="preview-annotation-overlay" 
                    :viewBox="`0 0 ${getAssetDimensions(previewAsset.assetId).width} ${getAssetDimensions(previewAsset.assetId).height}`"
                >
                    <g v-for="annotation in getVisibleAnnotations(previewAsset.assetId)" :key="annotation.annotationId">
                        <!-- Bounding box annotations -->
                        <rect 
                            v-if="isBoundingBoxAnnotation(annotation)"
                            :x="annotation.coordinates.topLeft.x"
                            :y="annotation.coordinates.topLeft.y"
                            :width="annotation.coordinates.bottomRight.x - annotation.coordinates.topLeft.x"
                            :height="annotation.coordinates.bottomRight.y - annotation.coordinates.topLeft.y"
                            fill="none"
                            stroke="#3b82f6"
                            stroke-width="2"
                            opacity="0.8"
                        />
                        <!-- Point annotations -->
                        <circle 
                            v-if="isPointAnnotation(annotation)"
                            :cx="annotation.coordinates.point.x"
                            :cy="annotation.coordinates.point.y"
                            r="8"
                            fill="#3b82f6"
                            opacity="0.8"
                            stroke="white"
                            stroke-width="1"
                        />
                        <!-- Polygon annotations -->
                        <polygon 
                            v-if="isPolygonAnnotation(annotation)"
                            :points="getPolygonPoints(annotation)"
                            fill="rgba(59, 130, 246, 0.3)"
                            stroke="#3b82f6"
                            stroke-width="2"
                            opacity="0.8"
                        />
                        <!-- Polyline annotations -->
                        <polyline 
                            v-if="isPolylineAnnotation(annotation)"
                            :points="getPolygonPoints(annotation)"
                            fill="none"
                            stroke="#3b82f6"
                            stroke-width="2"
                            opacity="0.8"
                        />
                        <!-- Line annotations -->
                        <line 
                            v-if="isLineAnnotation(annotation)"
                            :x1="annotation.coordinates.pointFrom.x"
                            :y1="annotation.coordinates.pointFrom.y"
                            :x2="annotation.coordinates.pointTo.x"
                            :y2="annotation.coordinates.pointTo.y"
                            stroke="#3b82f6"
                            stroke-width="2"
                            opacity="0.8"
                        />
                    </g>
                </svg>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';
import { faRefresh, faExclamationTriangle, faShapes } from '@fortawesome/free-solid-svg-icons';
import type { TaskTableRow } from '@/services/project/task/task.types';
import { useAssetPreview } from '@/composables/useAssetPreview';

interface Props {
    visible: boolean;
    previewAsset: TaskTableRow | null;
    popupStyle: Record<string, string>;
    previewImageLoaded: boolean;
    projectId: number;
}

interface Emits {
    (e: 'preview-image-load'): void;
    (e: 'preview-image-error', event: Event): void;
}

defineProps<Props>();
const emit = defineEmits<Emits>();

const {
    loadedAssets,
    loadingAssets,
    getAssetFullUrl,
    getAnnotationCount,
    getVisibleAnnotations,
    isBoundingBoxAnnotation,
    isPointAnnotation,
    isPolygonAnnotation,
    isPolylineAnnotation,
    isLineAnnotation,
    getPolygonPoints,
    getAssetDimensions,
    hasImageError,
    formatFileSize
} = useAssetPreview();

const onPreviewImageLoad = () => {
    emit('preview-image-load');
};

const onPreviewImageError = (event: Event) => {
    emit('preview-image-error', event);
};
</script>

<style lang="scss" scoped>
.asset-preview-popup {
    position: fixed;
    background: var(--color-white);
    border: 1px solid var(--color-gray-300);
    border-radius: 8px;
    box-shadow: 0 10px 25px rgba(0, 0, 0, 0.15);
    padding: 0;
    z-index: 1000;
    max-width: 400px;
    overflow: hidden;
    
    .preview-header {
        padding: 0.75rem 1rem;
        background: var(--color-gray-100);
        border-bottom: 1px solid var(--color-gray-300);
        
        .preview-asset-name {
            font-weight: 500;
            color: var(--color-gray-800);
            font-size: 0.875rem;
            display: block;
            margin-bottom: 0.25rem;
        }
        
        .asset-metadata {
            display: flex;
            gap: 0.75rem;
            font-size: 0.75rem;
            color: var(--color-gray-600);
            
            .asset-size,
            .asset-dimensions,
            .annotation-info {
                display: flex;
                align-items: center;
                gap: 0.25rem;
            }
            
            .annotation-info {
                color: var(--color-primary);
                font-weight: 500;
            }
        }
    }
    
    .preview-image-container {
        padding: 1rem;
        display: flex;
        justify-content: center;
        align-items: center;
        background: var(--color-gray-50);
        min-height: 200px;
        
        .loading-state {
            display: flex;
            flex-direction: column;
            align-items: center;
            gap: 0.75rem;
            color: var(--color-gray-600);
            
            .loading-icon-large {
                font-size: 1.5rem;
                color: var(--color-primary);
            }
        }
        
        .error-state {
            display: flex;
            flex-direction: column;
            align-items: center;
            gap: 0.75rem;
            color: var(--color-gray-600);
            padding: 2rem;
            
            .error-icon-large {
                font-size: 2rem;
                color: var(--color-error);
            }
        }
        
        .preview-image-wrapper {
            position: relative;
            display: flex;
            justify-content: center;
            align-items: center;
        }
        
        .preview-image {
            max-width: 100%;
            max-height: 300px;
            border-radius: 4px;
            box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
            background: var(--color-gray-100);
            min-width: 200px;
            min-height: 150px;
            
            &.image-error {
                opacity: 1;
                background: var(--color-gray-200);
            }
            
            // Handle empty src images
            &:not([src]),
            &[src=""] {
                background: var(--color-gray-100);
                opacity: 0.9;
            }
        }
        
        .preview-annotation-overlay {
            position: absolute;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            pointer-events: none;
            z-index: 1;
        }
    }
}
</style>