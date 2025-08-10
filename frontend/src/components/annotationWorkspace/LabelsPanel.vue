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
    </div>
</template>

<script setup lang="ts">
import {computed} from 'vue';
import {useWorkspaceStore} from '@/stores/workspaceStore';

const workspaceStore = useWorkspaceStore();

const isLoading = computed(() => workspaceStore.getLoadingState);
const availableLabels = computed(() => workspaceStore.getAvailableLabels);
const selectedLabelId = computed(() => workspaceStore.getSelectedLabelId);

const selectLabel = (labelId: number) => {
    if (selectedLabelId.value === labelId) {
        // Deselect if clicking the same label
        workspaceStore.setCurrentLabelId(null);
    } else {
        workspaceStore.setCurrentLabelId(labelId);
    }
};
</script>

<style scoped>
.labels-panel {
    display: flex;
    flex-direction: column;
    height: 100%;
    padding: 1rem;
}

.panel-title {
    color: var(--color-gray-200);
    font-size: 1.25rem;
    margin-bottom: 1rem;
    border-bottom: 1px solid var(--color-accent-blue);
    padding-bottom: 0.5rem;
}

.scheme-selector {
    margin-bottom: 1rem;
    padding: 0.75rem;
    background-color: var(--color-dark-blue-2);
    border-radius: 4px;
    border: 1px solid var(--color-accent-blue);
}

.selector-label {
    display: block;
    color: var(--color-gray-300);
    font-size: 0.875rem;
    font-weight: bold;
    margin-bottom: 0.5rem;
}

.scheme-dropdown {
    width: 100%;
    padding: 0.5rem;
    background-color: var(--color-dark-blue-3);
    color: var(--color-gray-200);
    border: 1px solid var(--color-gray-600);
    border-radius: 4px;
    font-size: 0.875rem;
    outline: none;
    transition: border-color 0.2s ease-in-out;
    
    &:focus {
        border-color: var(--color-primary);
    }
    
    &:hover {
        border-color: var(--color-accent-blue);
    }
}

.scheme-dropdown option {
    background-color: var(--color-dark-blue-3);
    color: var(--color-gray-200);
}

.panel-loading {
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 1.5rem;
    color: var(--color-gray-400);
    font-style: italic;
}

.no-labels {
    padding: 1.5rem;
    text-align: center;
    color: var(--color-gray-400);
    
    p {
        margin-bottom: 0.5rem;
        font-weight: bold;
    }
    
    small {
        color: var(--color-gray-500);
        line-height: 1.4;
    }
}

.labels-list {
    flex: 1;
    overflow-y: auto;
    margin-bottom: 1rem;
}

.label-item {
    display: flex;
    flex-direction: column;
    padding: 0.5rem;
    margin-bottom: 0.5rem;
    border-radius: 4px;
    background-color: var(--color-dark-blue-3);
    border: 2px solid transparent;
    cursor: pointer;
    transition: all 0.2s ease-in-out;
    
    &:hover {
        background-color: rgba(var(--color-accent-blue), 0.2);
        border-color: var(--color-accent-blue);
    }
    
    &.label-selected {
        border-color: var(--color-primary);
        background-color: rgba(var(--color-primary), 0.1);
    }
}

.label-color-indicator {
    width: 20px;
    height: 20px;
    border-radius: 4px;
    margin-bottom: 0.5rem;
    border: 1px solid var(--color-gray-600);
    flex-shrink: 0;
}

.label-name {
    color: var(--color-gray-200);
    font-weight: bold;
    margin-bottom: 0.25rem;
}

.label-description {
    color: var(--color-gray-400);
    font-size: 0.875rem;
    line-height: 1.3;
}

.scheme-info {
    border-top: 1px solid var(--color-accent-blue);
    padding-top: 0.5rem;
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
}

.scheme-name {
    color: var(--color-gray-300);
    font-weight: bold;
}

.scheme-description {
    color: var(--color-gray-400);
    line-height: 1.3;
}
</style>
