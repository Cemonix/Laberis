<template>
    <div class="tools-panel-left">
        <div class="panel-section">
            <h3 class="section-title">Tools</h3>
            <div class="tools-grid">
                <button
                    v-for="tool in availableTools"
                    :key="tool.id"
                    class="tool-button"
                    :class="{ 'active-tool': tool.id === activeToolId }"
                    @click="selectTool(tool.id)"
                    :title="tool.name"
                >
                    <font-awesome-icon v-if="tool.iconDefinition" :icon="tool.iconDefinition" class="tool-icon" />
                </button>
            </div>
        </div>

        <div class="panel-section" v-if="currentLabelScheme && currentLabelScheme.labels.length > 0">
            <h3 class="section-title">Labels</h3>
            <div class="labels-list">
                <button
                    v-for="label in currentLabelScheme.labels"
                    :key="label.labelId"
                    class="label-button"
                    :class="{ 'active-label': label.labelId === selectedLabelId }"
                    @click="selectLabel(label.labelId)"
                    :title="label.description || label.name"
                >
                    <span class="label-color-swatch" :style="{ backgroundColor: label.color }"></span>
                    <span class="label-name">{{ label.name }}</span>
                </button>
            </div>
        </div>
        <div class="panel-section" v-else-if="currentLabelScheme && currentLabelScheme.labels.length === 0">
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
import { computed } from 'vue';
import { useWorkspaceStore } from '@/stores/workspaceStore';
import type { ToolName } from '@/types/workspace/tools';
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';

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

<style lang="scss" scoped>
@use "sass:color";
@use "@/styles/variables.scss" as vars;

.tools-panel-left {
    padding: vars.$padding-small;
    display: flex;
    flex-direction: column;
    gap: vars.$padding-medium;
    background-color: vars.$workspace-panel-bg;
    color: vars.$workspace-container-text;
    overflow-y: auto;
}

.panel-section {
    display: flex;
    flex-direction: column;
    gap: vars.$padding-small;
}

.section-title {
    font-size: 0.9rem;
    font-weight: bold;
    color: vars.$workspace-container-text;
    text-transform: uppercase;
    margin-bottom: calc(vars.$padding-small / 2);
    border-bottom: 1px solid color.adjust(vars.$workspace-border-color, $lightness: 10%);
    padding-bottom: calc(vars.$padding-small / 2);
}

.tools-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(40px, 1fr));
    gap: vars.$padding-small;
}

.tool-button {
    display: flex;
    justify-content: center;
    align-items: center;
    width: 100%;
    aspect-ratio: 1 / 1;
    padding: vars.$padding-small;
    background-color: color.adjust(vars.$workspace-panel-bg, $lightness: -5%);
    color: vars.$workspace-container-text;
    border: 1px solid vars.$workspace-border-color;
    border-radius: vars.$border-radius-standard;
    cursor: pointer;
    transition: background-color vars.$transition-fast, border-color vars.$transition-fast;

    &:hover {
        background-color: color.adjust(vars.$workspace-panel-bg, $lightness: 5%);
        border-color: color.adjust(vars.$workspace-border-color, $lightness: 10%);
    }

    &.active-tool {
        background-color: vars.$primary-blue;
        color: white;
        border-color: color.adjust(vars.$primary-blue, $lightness: -10%);
    }

    .tool-icon {
        font-size: 1.25rem;
    }
}

.labels-list {
    display: flex;
    flex-direction: column;
    gap: calc(vars.$padding-small / 2);
}

.label-button {
    display: flex;
    align-items: center;
    width: 100%;
    padding: calc(vars.$padding-small * 0.75) vars.$padding-small;
    background-color: color.adjust(vars.$workspace-panel-bg, $lightness: -3%);
    color: vars.$workspace-container-text;
    border: 1px solid vars.$workspace-border-color;
    border-radius: vars.$border-radius-standard;
    cursor: pointer;
    text-align: left;
    font-size: 0.85rem;
    transition: background-color vars.$transition-fast, border-color vars.$transition-fast;

    &:hover {
        background-color: color.adjust(vars.$workspace-panel-bg, $lightness: 5%);
        border-color: color.adjust(vars.$workspace-border-color, $lightness: 10%);
    }

    &.active-label {
        background-color: vars.$primary-blue;
        color: white;
        border-color: color.adjust(vars.$primary-blue, $lightness: -10%);
        font-weight: bold;

        .label-color-swatch {
            border-color: rgba(255,255,255,0.7);
        }
    }
}

.label-color-swatch {
    width: 12px;
    height: 12px;
    border-radius: 3px;
    margin-right: vars.$padding-small;
    border: 1px solid vars.$workspace-border-color;
    flex-shrink: 0;
}

.label-name {
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}

.no-labels-message {
    font-size: 0.8rem;
    color: color.adjust(vars.$workspace-container-text, $lightness: 20%);
    padding: vars.$padding-small;
    text-align: center;
}
</style>