<template>
    <router-link v-if="asset" :to="workspaceUrl" class="asset-thumbnail-link">
        <Card class="asset-card">
            <div class="asset-image-wrapper">
                <img 
                    v-if="asset.imageUrl" 
                    :src="asset.imageUrl" 
                    :alt="`Thumbnail for ${asset.filename}`" 
                    class="asset-image" 
                    loading="lazy"
                    @error="handleImageError"
                />
                <div v-else class="error-placeholder">
                    <span>Image processing...</span>
                </div>
                <div class="image-overlay">
                    <div class="overlay-actions">
                        <span class="overlay-text">Annotate</span>
                        <!-- TODO: Implement full annotation preview with bounding boxes/masks -->
                        <Button 
                            v-if="hasAnnotations" 
                            variant="secondary" 
                            size="small" 
                            @click.prevent="togglePreview"
                            class="preview-button"
                        >
                            {{ showPreview ? 'Hide' : 'Preview' }}
                        </Button>
                    </div>
                </div>
                <div v-if="showPreview && hasAnnotations" class="annotation-preview">
                    <div class="annotation-count">{{ annotationCount }} annotation{{ annotationCount !== 1 ? 's' : '' }}</div>
                </div>
            </div>
            <template #footer>
                <div class="asset-info">
                    <span class="asset-name" :title="asset.filename">{{ asset.filename }}</span>
                    <span class="asset-status" :class="`status-${asset.status}`">{{ asset.status.replace('_', ' ') }}</span>
                </div>
            </template>
        </Card>
    </router-link>
    <div v-else class="asset-thumbnail-error">
        <Card class="asset-card">
            <div class="asset-image-wrapper">
                <div class="error-placeholder">
                    <span>Asset not available</span>
                </div>
            </div>
            <template #footer>
                <div class="asset-info">
                    <span class="asset-name">Unknown Asset</span>
                    <span class="asset-status">Error</span>
                </div>
            </template>
        </Card>
    </div>
</template>

<script setup lang="ts">
import {computed, ref} from 'vue';
import {useRoute} from 'vue-router';
import type {Asset} from '@/types/asset/asset';
import Card from '@/components/common/Card.vue';
import Button from '@/components/common/Button.vue';
import {AppLogger} from '@/utils/logger';

const logger = AppLogger.createComponentLogger('AssetThumbnail');

const props = defineProps<{
    asset?: Asset | null;
}>();

const route = useRoute();
const projectId = route.params.projectId;

const workspaceUrl = computed(() => {
    if (!props.asset?.id) {
        return '#';
    }
    return `/workspace/project/${projectId}/asset/${props.asset.id}`;
});

const showPreview = ref(false);

const hasAnnotations = computed(() => {
    // This would be determined by actual annotation data
    // For now, we'll consider assets with 'annotated' or 'review' statuses to have annotations
    const annotatedStatuses = ['annotated', 'pending_review', 'review_in_progress', 'review_accepted'];
    return props.asset && annotatedStatuses.includes(props.asset.status.toLowerCase());
});

const annotationCount = computed(() => {
    // TODO: Replace with real annotation count from API
    // This would come from actual annotation data loaded from the asset service
    if (!hasAnnotations.value) return 0;
    return Math.floor(Math.random() * 5) + 1; // Temporary simulation
});

const togglePreview = () => {
    showPreview.value = !showPreview.value;
};

const handleImageError = () => {
    logger.warn(`Failed to load image for asset ${props.asset?.id}: URL ${props.asset?.imageUrl}`);
};
</script>

<style scoped>
.asset-thumbnail-link {
    display: block;
    text-decoration: none;
    color: inherit;
    height: 100%;
}

.asset-thumbnail-error {
    display: block;
    height: 100%;
}

.error-placeholder {
    display: flex;
    justify-content: center;
    align-items: center;
    width: 100%;
    height: 100%;
    background-color: var(--color-gray-100);
    color: var(--color-gray-600);
    font-style: italic;
}

.asset-card {
    overflow: hidden;
    transition: transform 0.2s ease-in-out, box-shadow 0.2s ease-in-out;
}

.asset-card:hover {
    transform: translateY(-5px);
    box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.asset-card .asset-image-wrapper {
    position: relative;
    width: 100%;
    aspect-ratio: 16 / 10;
    background-color: var(--color-gray-200);
}

.asset-card .asset-image-wrapper .asset-image {
    width: 100%;
    height: 100%;
    object-fit: cover;
}

.asset-card .asset-image-wrapper .image-overlay {
    position: absolute;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    background-color: rgba(0, 0, 0, 0.5);
    color: var(--color-white);
    display: flex;
    justify-content: center;
    align-items: center;
    opacity: 0;
    transition: opacity 0.2s ease-in-out;
    font-weight: bold;
}

.overlay-actions {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 0.5rem;
}

.preview-button {
    font-size: 0.75rem;
    padding: 0.25rem 0.5rem;
}

.annotation-preview {
    position: absolute;
    bottom: 0;
    left: 0;
    right: 0;
    background: linear-gradient(to top, rgba(0, 0, 0, 0.8), transparent);
    color: var(--color-white);
    padding: 0.5rem;
    font-size: 0.75rem;
    text-align: center;
}

.annotation-count {
    font-weight: bold;
}

.asset-card:hover .image-overlay {
    opacity: 1;
}

.asset-card :deep(.base-card-footer) {
    padding: 0.5rem;
}

.asset-info {
    display: flex;
    justify-content: space-between;
    align-items: center;
    width: 100%;
}

.asset-name {
    font-size: 0.875rem;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    flex-grow: 1;
    margin-right: 0.5rem;
}

.asset-status {
    font-size: 0.75rem;
    font-weight: bold;
    padding: 0.2em 0.6em;
    border-radius: 1em;
    text-transform: capitalize;
    flex-shrink: 0;
}

.asset-status.status-new,
.asset-status.status-ready_for_annotation {
    background-color: var(--color-blue-100);
    color: var(--color-blue-800);
}

.asset-status.status-annotated,
.asset-status.status-annotation_in_progress {
    background-color: var(--color-purple-100);
    color: var(--color-purple-800);
}

.asset-status.status-pending_review,
.asset-status.status-review_in_progress {
    background-color: var(--color-yellow-100);
    color: var(--color-yellow-800);
}

.asset-status.status-review_accepted,
.asset-status.status-exported {
    background-color: var(--color-green-100);
    color: var(--color-green-800);
}

.asset-status.status-review_rejected,
.asset-status.status-import_error,
.asset-status.status-processing_error {
    background-color: var(--color-red-100);
    color: var(--color-red-800);
}

.asset-status.status-imported,
.asset-status.status-pending_processing,
.asset-status.status-processing {
    background-color: var(--color-gray-100);
    color: var(--color-gray-800);
}
</style>