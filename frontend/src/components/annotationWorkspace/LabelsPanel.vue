<template>
    <div class="labels-panel">
        <h3 class="panel-title">Labels</h3>
        
        <!-- Loading state -->
        <div v-if="isLoading" class="panel-loading">
            <span>Loading labels...</span>
        </div>
        
        <!-- No labels state -->
        <div v-else-if="!availableLabels || availableLabels.length === 0" class="no-labels">
            <p>No labels available</p>
            <small>Create a label scheme for this project to start annotating.</small>
        </div>
        
        <!-- Labels list -->
        <div v-else class="labels-list">
            <div 
                v-for="label in availableLabels" 
                :key="label.labelId"
                :class="['label-item', { 'label-selected': label.labelId === selectedLabelId }]"
                @click="selectLabel(label.labelId)"
                :title="label.description || label.name"
            >
                <div 
                    class="label-color-indicator" 
                    :style="{ backgroundColor: label.color }"
                ></div>
                <span class="label-name">{{ label.name }}</span>
                <small v-if="label.description" class="label-description">{{ label.description }}</small>
            </div>
        </div>
        
        <!-- Current label scheme info -->
        <div v-if="currentLabelScheme" class="scheme-info">
            <small class="scheme-name">Scheme: {{ currentLabelScheme.name }}</small>
            <small v-if="currentLabelScheme.description" class="scheme-description">{{ currentLabelScheme.description }}</small>
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useWorkspaceStore } from '@/stores/workspaceStore';

const workspaceStore = useWorkspaceStore();

const isLoading = computed(() => workspaceStore.getLoadingState);
const availableLabels = computed(() => workspaceStore.getAvailableLabels);
const selectedLabelId = computed(() => workspaceStore.getSelectedLabelId);
const currentLabelScheme = computed(() => workspaceStore.getCurrentLabelScheme);

const selectLabel = (labelId: number) => {
    if (selectedLabelId.value === labelId) {
        // Deselect if clicking the same label
        workspaceStore.setCurrentLabelId(null);
    } else {
        workspaceStore.setCurrentLabelId(labelId);
    }
};
</script>

<style lang="scss" scoped>
@use "@/styles/variables" as vars;

.labels-panel {
    display: flex;
    flex-direction: column;
    height: 100%;
    padding: vars.$padding-medium;
}

.panel-title {
    color: vars.$color-gray-200;
    font-size: vars.$font-size-large;
    margin-bottom: vars.$margin-medium;
    border-bottom: 1px solid vars.$color-accent-blue;
    padding-bottom: vars.$padding-small;
}

.panel-loading {
    display: flex;
    align-items: center;
    justify-content: center;
    padding: vars.$padding-large;
    color: vars.$color-gray-400;
    font-style: italic;
}

.no-labels {
    padding: vars.$padding-large;
    text-align: center;
    color: vars.$color-gray-400;
    
    p {
        margin-bottom: vars.$margin-small;
        font-weight: bold;
    }
    
    small {
        color: vars.$color-gray-500;
        line-height: 1.4;
    }
}

.labels-list {
    flex: 1;
    overflow-y: auto;
    margin-bottom: vars.$margin-medium;
}

.label-item {
    display: flex;
    flex-direction: column;
    padding: vars.$padding-small;
    margin-bottom: vars.$margin-small;
    border-radius: vars.$border-radius;
    background-color: vars.$color-dark-blue-3;
    border: 2px solid transparent;
    cursor: pointer;
    transition: all 0.2s ease-in-out;
    
    &:hover {
        background-color: rgba(vars.$color-accent-blue, 0.2);
        border-color: vars.$color-accent-blue;
    }
    
    &.label-selected {
        border-color: vars.$color-primary;
        background-color: rgba(vars.$color-primary, 0.1);
    }
}

.label-color-indicator {
    width: 20px;
    height: 20px;
    border-radius: 4px;
    margin-bottom: vars.$margin-small;
    border: 1px solid vars.$color-gray-600;
    flex-shrink: 0;
}

.label-name {
    color: vars.$color-gray-200;
    font-weight: bold;
    margin-bottom: vars.$margin-tiny;
}

.label-description {
    color: vars.$color-gray-400;
    font-size: vars.$font-size-small;
    line-height: 1.3;
}

.scheme-info {
    border-top: 1px solid vars.$color-accent-blue;
    padding-top: vars.$padding-small;
    display: flex;
    flex-direction: column;
    gap: vars.$gap-tiny;
}

.scheme-name {
    color: vars.$color-gray-300;
    font-weight: bold;
}

.scheme-description {
    color: vars.$color-gray-400;
    line-height: 1.3;
}
</style>
