<template>
    <div class="asset-preview-cell" @mouseenter="onMouseEnter" @mouseleave="onMouseLeave">
        <div class="asset-thumbnail">
            <img 
                :src="getAssetThumbnailUrl(projectId, task.assetId)" 
                :alt="task.assetName"
                class="thumbnail-image"
                @error="onImageError"
                :class="{ 'loading': loadingAssets.has(task.assetId) }"
            />
            <div v-if="loadingAssets.has(task.assetId)" class="loading-overlay">
                <font-awesome-icon :icon="faRefresh" spin class="loading-icon" />
            </div>
            <div v-if="hasImageError(task.assetId)" class="error-overlay">
                <font-awesome-icon :icon="faExclamationTriangle" class="error-icon" />
                <span class="error-text">Image Error</span>
            </div>
            <!-- Annotation indicators -->
            <div v-if="getAnnotationCount(task.assetId) > 0" class="annotation-indicator">
                <font-awesome-icon :icon="faShapes" class="annotation-icon" />
                <span class="annotation-count">{{ getAnnotationCount(task.assetId) }}</span>
            </div>
            <!-- Annotation overlay -->
            <svg 
                v-if="getAnnotationCount(task.assetId) > 0" 
                class="annotation-overlay" 
                viewBox="0 0 128 128"
            >
                <g v-for="annotation in getVisibleAnnotations(task.assetId)" :key="annotation.annotationId">
                    <!-- Bounding box annotations -->
                    <rect 
                        v-if="isBoundingBoxAnnotation(annotation)"
                        :x="scaleCoordinate(annotation.coordinates.topLeft.x, loadedAssets.get(task.assetId)?.width, 128)"
                        :y="scaleCoordinate(annotation.coordinates.topLeft.y, loadedAssets.get(task.assetId)?.height, 128)"
                        :width="scaleCoordinate(annotation.coordinates.bottomRight.x - annotation.coordinates.topLeft.x, loadedAssets.get(task.assetId)?.width, 128)"
                        :height="scaleCoordinate(annotation.coordinates.bottomRight.y - annotation.coordinates.topLeft.y, loadedAssets.get(task.assetId)?.height, 128)"
                        fill="none"
                        stroke="#3b82f6"
                        stroke-width="1.5"
                        opacity="0.8"
                    />
                    <!-- Point annotations -->
                    <circle 
                        v-if="isPointAnnotation(annotation)"
                        :cx="scaleCoordinate(annotation.coordinates.point.x, loadedAssets.get(task.assetId)?.width, 128)"
                        :cy="scaleCoordinate(annotation.coordinates.point.y, loadedAssets.get(task.assetId)?.height, 128)"
                        r="3"
                        fill="#3b82f6"
                        opacity="0.8"
                    />
                    <!-- Polygon annotations -->
                    <polygon 
                        v-if="isPolygonAnnotation(annotation)"
                        :points="getScaledPolygonPoints(annotation, task.assetId, 128)"
                        fill="rgba(59, 130, 246, 0.3)"
                        stroke="#3b82f6"
                        stroke-width="1.5"
                        opacity="0.8"
                    />
                    <!-- Polyline annotations -->
                    <polyline 
                        v-if="isPolylineAnnotation(annotation)"
                        :points="getScaledPolygonPoints(annotation, task.assetId, 128)"
                        fill="none"
                        stroke="#3b82f6"
                        stroke-width="1.5"
                        opacity="0.8"
                    />
                    <!-- Line annotations -->
                    <line 
                        v-if="isLineAnnotation(annotation)"
                        :x1="scaleCoordinate(annotation.coordinates.pointFrom.x, loadedAssets.get(task.assetId)?.width, 128)"
                        :y1="scaleCoordinate(annotation.coordinates.pointFrom.y, loadedAssets.get(task.assetId)?.height, 128)"
                        :x2="scaleCoordinate(annotation.coordinates.pointTo.x, loadedAssets.get(task.assetId)?.width, 128)"
                        :y2="scaleCoordinate(annotation.coordinates.pointTo.y, loadedAssets.get(task.assetId)?.height, 128)"
                        stroke="#3b82f6"
                        stroke-width="1.5"
                        opacity="0.8"
                    />
                </g>
            </svg>
        </div>
    </div>
</template>

<script setup lang="ts">
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';
import { faRefresh, faExclamationTriangle, faShapes } from '@fortawesome/free-solid-svg-icons';
import type { TaskTableRow } from '@/types/task';
import { useAssetPreview } from '@/composables/useAssetPreview';

interface Props {
    task: TaskTableRow;
    projectId: number;
    allTasks: TaskTableRow[];
}

interface Emits {
    (e: 'preview-show', event: MouseEvent, task: TaskTableRow): void;
    (e: 'preview-hide'): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

const {
    loadedAssets,
    loadingAssets,
    getAssetThumbnailUrl,
    getAnnotationCount,
    getVisibleAnnotations,
    isBoundingBoxAnnotation,
    isPointAnnotation,
    isPolygonAnnotation,
    isPolylineAnnotation,
    isLineAnnotation,
    scaleCoordinate,
    getScaledPolygonPoints,
    hasImageError,
    handleImageError
} = useAssetPreview();

const onMouseEnter = (event: MouseEvent) => {
    emit('preview-show', event, props.task);
};

const onMouseLeave = () => {
    emit('preview-hide');
};

const onImageError = (event: Event) => {
    handleImageError(event, props.allTasks);
};
</script>

<style lang="scss" scoped>
.asset-preview-cell {
    display: flex;
    align-items: center;
    justify-content: center;
    cursor: pointer;
    padding: 0.5rem;
    
    .asset-thumbnail {
        position: relative;
        width: 128px;
        height: 128px;
        border-radius: 8px;
        overflow: hidden;
        border: 2px solid var(--color-gray-300);
        flex-shrink: 0;
        box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
        
        .thumbnail-image {
            width: 100%;
            height: 100%;
            object-fit: cover;
            transition: transform 0.2s;
            background: var(--color-gray-100);
            
            &.loading {
                opacity: 0.6;
            }
            
            &.image-error {
                opacity: 1;
                background: var(--color-gray-200);
            }
            
            // Handle empty src images
            &:not([src]),
            &[src=""] {
                background: var(--color-gray-100);
                opacity: 0.8;
            }
        }
        
        .loading-overlay {
            position: absolute;
            top: 0;
            left: 0;
            right: 0;
            bottom: 0;
            display: flex;
            align-items: center;
            justify-content: center;
            background: var(--color-gray-100);
            border-radius: 6px;
            
            .loading-icon {
                color: var(--color-primary);
                font-size: 1.5rem;
            }
        }
        
        .error-overlay {
            position: absolute;
            top: 0;
            left: 0;
            right: 0;
            bottom: 0;
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            background: var(--color-gray-200);
            border-radius: 6px;
            gap: 0.25rem;
            
            .error-icon {
                color: var(--color-error);
                font-size: 1.25rem;
            }
            
            .error-text {
                color: var(--color-gray-600);
                font-size: 0.75rem;
                font-weight: 500;
                text-align: center;
            }
        }
        
        .annotation-indicator {
            position: absolute;
            top: 4px;
            right: 4px;
            background: rgba(59, 130, 246, 0.9);
            color: white;
            border-radius: 12px;
            padding: 2px 6px;
            font-size: 0.75rem;
            font-weight: 600;
            display: flex;
            align-items: center;
            gap: 2px;
            box-shadow: 0 2px 4px rgba(0, 0, 0, 0.2);
            
            .annotation-icon {
                font-size: 0.7rem;
            }
            
            .annotation-count {
                line-height: 1;
            }
        }
        
        .annotation-overlay {
            position: absolute;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            pointer-events: none;
            z-index: 1;
        }
    }
    
    &:hover {
        .asset-thumbnail {
            border-color: var(--color-primary);
            
            .thumbnail-image:not(.loading) {
                transform: scale(1.02);
            }
        }
    }
}
</style>