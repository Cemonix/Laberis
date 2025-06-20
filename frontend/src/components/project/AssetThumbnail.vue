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
                    <span class="overlay-text">Annotate</span>
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
import { computed } from 'vue';
import { useRoute } from 'vue-router';
import type { Asset } from '@/types/asset/asset';
import Card from '@/components/common/Card.vue';
import { AppLogger } from '@/utils/logger';

const logger = AppLogger.createServiceLogger('AssetThumbnail');

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

const handleImageError = () => {
    logger.warn(`Failed to load image for asset ${props.asset?.id}: URL ${props.asset?.imageUrl}`);
};
</script>

<style lang="scss" scoped>
@use "sass:color";
@use "@/styles/variables" as vars;

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
    background-color: vars.$color-gray-100;
    color: vars.$color-gray-600;
    font-style: italic;
}

.asset-card {
    overflow: hidden;
    transition: transform 0.2s ease-in-out, box-shadow 0.2s ease-in-out;

    &:hover {
        transform: translateY(-5px);
        box-shadow: vars.$shadow-md;
    }
    
    .asset-image-wrapper {
        position: relative;
        width: 100%;
        aspect-ratio: 16 / 10;
        background-color: vars.$color-gray-200;

        .asset-image {
            width: 100%;
            height: 100%;
            object-fit: cover;
        }
        
        .image-overlay {
            position: absolute;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            background-color: rgba(vars.$color-black, 0.5);
            color: vars.$color-white;
            display: flex;
            justify-content: center;
            align-items: center;
            opacity: 0;
            transition: opacity 0.2s ease-in-out;
            font-weight: bold;
        }
    }

    &:hover .image-overlay {
        opacity: 1;
    }

    :deep(.base-card-footer) {
        padding: vars.$padding-small;
    }
}

.asset-info {
    display: flex;
    justify-content: space-between;
    align-items: center;
    width: 100%;
}

.asset-name {
    font-size: vars.$font-size-small;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    flex-grow: 1;
    margin-right: vars.$margin-small;
}

.asset-status {
    font-size: 0.75rem;
    font-weight: bold;
    padding: 0.2em 0.6em;
    border-radius: 1em;
    text-transform: capitalize;
    flex-shrink: 0;

    &.status-new {
        background-color: color.adjust(vars.$color-info, $alpha: -0.8);
        color: vars.$color-info;
    }
    &.status-annotated {
        background-color: color.adjust(vars.$color-primary, $alpha: -0.8);
        color: vars.$color-primary;
    }
    &.status-in_review {
        background-color: color.adjust(vars.$color-warning, $alpha: -0.8);
        color: vars.$color-warning;
    }
     &.status-approved {
        background-color: color.adjust(vars.$color-success, $alpha: -0.8);
        color: vars.$color-success;
    }
}
</style>