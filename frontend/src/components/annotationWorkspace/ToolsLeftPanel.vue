<template>
    <div class="tools-panel-left">
        <div class="panel-section">
            <h3 class="section-title">Tools</h3>
            <div class="tools-grid">
                <Button
                    v-for="tool in availableTools"
                    :key="tool.id"
                    class="tool-button"
                    :class="{ 'active-tool': tool.id === activeToolId }"
                    @click="selectTool(tool.id)"
                    :title="tool.name"
                >
                    <font-awesome-icon v-if="tool.iconDefinition" :icon="tool.iconDefinition" class="tool-icon" />
                </Button>
            </div>
        </div>

        <div class="panel-section" v-if="currentLabelScheme && currentLabelScheme.labels && currentLabelScheme.labels.length > 0">
            <h3 class="section-title">Labels</h3>
            <div class="labels-list">
                <Button
                    v-for="label in currentLabelScheme.labels"
                    :key="label.labelId"
                    class="label-button"
                    :class="{ 'active-label': label.labelId === selectedLabelId }"
                    @click="selectLabel(label.labelId)"
                    :title="label.description || label.name"
                >
                    <span class="label-color-swatch" :style="{ backgroundColor: label.color }"></span>
                    <span class="label-name">{{ label.name }}</span>
                </Button>
            </div>
        </div>
        <div class="panel-section" v-else-if="currentLabelScheme && currentLabelScheme.labels && currentLabelScheme.labels.length === 0">
            <h3 class="section-title">Labels</h3>
            <p class="no-labels-message">No labels in the current scheme.</p>
        </div>
        <div class="panel-section" v-else>
            <h3 class="section-title">Labels</h3>
            <p class="no-labels-message">No label scheme loaded.</p>
        </div>
        </div>
</template>

<script setup lang="ts">
import {computed} from 'vue';
import {useWorkspaceStore} from '@/stores/workspaceStore';
import type {ToolName} from '@/types/workspace/tools';
import {FontAwesomeIcon} from '@fortawesome/vue-fontawesome';
import Button from '@/components/common/Button.vue';

const workspaceStore = useWorkspaceStore();

const availableTools = computed(() => workspaceStore.availableTools);
const activeToolId = computed(() => workspaceStore.activeTool);

const selectTool = (toolId: ToolName) => {
    workspaceStore.setActiveTool(toolId);
};

const currentLabelScheme = computed(() => workspaceStore.getCurrentLabelScheme);
const selectedLabelId = computed(() => workspaceStore.getSelectedLabelId);

const selectLabel = (labelId: number) => {
    // Toggle behavior: if the same label is clicked again, deselect it. Otherwise, select the new one.
    if (selectedLabelId.value === labelId) {
        workspaceStore.setCurrentLabelId(null);
    } else {
        workspaceStore.setCurrentLabelId(labelId);
    }
};
</script>

<style scoped>
.tools-panel-left {
    display: flex;
    flex-direction: column;
    gap: 1rem;
    padding: 0.5rem;
    background-color: var(--color-dark-blue-2);
    color: var(--color-white);
    overflow-y: auto;
}

.panel-section {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.section-title {
    font-size: 0.875rem;
    font-weight: 700;
    color: var(--color-white);
    text-transform: uppercase;
    margin-bottom: 0.25rem;
    border-bottom: 1px solid var(--color-accent-blue);
    padding-bottom: 0.25rem;
}

.tools-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(30px, 1fr));
    gap: 0.5rem;
}

.tool-button {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 100%;
    aspect-ratio: 1 / 1;
    padding: 0.5rem;
    background-color: var(--color-dark-blue-2);
    color: var(--color-white);
    border: 1px solid var(--color-accent-blue);
    border-radius: 4px;
    cursor: pointer;
    transition: background-color 0.1s ease-in-out, border-color 0.1s ease-in-out;

    &:hover {
        background-color: var(--color-dark-blue-2);
        border-color: var(--color-accent-blue);
    }

    &.active-tool {
        background-color: var(--color-primary);
        color: var(--color-white);
        border-color: var(--color-primary);
    }

    .tool-icon {
        font-size: 1.25rem;
    }
}

.labels-list {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.label-button {
    display: flex;
    align-items: center;
    width: 100%;
    padding: calc(0.5rem * 0.75) 0.5rem;
    background-color: var(--color-dark-blue-2);
    color: var(--color-white);
    border: 1px solid var(--color-accent-blue);
    border-radius: 4px;
    cursor: pointer;
    text-align: left;
    font-size: 0.875rem;
    transition: background-color 0.1s ease-in-out, border-color 0.1s ease-in-out;

    &:hover {
        background-color: var(--color-dark-blue-2);
        border-color: var(--color-accent-blue);
    }

    &.active-label {
        background-color: var(--color-primary);
        color: var(--color-white);
        border-color: var(--color-primary);
        font-weight: 700;

        .label-color-swatch {
            border-color: rgba(var(--color-white), 0.7);
        }
    }
}

.label-color-swatch {
    width: 12px;
    height: 12px;
    border-radius: 3px;
    margin-right: 0.5rem;
    border: 1px solid var(--color-accent-blue);
    flex-shrink: 0;
}

.label-name {
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}

.no-labels-message {
    font-size: 0.875rem;
    color: var(--color-white);
    padding: 0.5rem;
    text-align: center;
}
</style>