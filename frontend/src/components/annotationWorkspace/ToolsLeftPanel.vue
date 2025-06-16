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
import { computed } from 'vue';
import { useWorkspaceStore } from '@/stores/workspaceStore';
import type { ToolName } from '@/types/workspace/tools';
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';
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

<style lang="scss" scoped>
@use "sass:color";
@use "@/styles/variables" as vars;

.tools-panel-left {
    display: flex;
    flex-direction: column;
    gap: vars.$gap-medium;
    padding: vars.$padding-small;
    background-color: vars.$color-dark-blue-2;
    color: vars.$color-white;
    overflow-y: auto;
}

.panel-section {
    display: flex;
    flex-direction: column;
    gap: vars.$gap-small;
}

.section-title {
    font-size: vars.$font_size_small;
    font-weight: vars.$font-weight-heading;
    color: vars.$color-white;
    text-transform: uppercase;
    margin-bottom: vars.$margin-tiny;
    border-bottom: 1px solid color.adjust(vars.$color-accent-blue, $lightness: 10%);
    padding-bottom: vars.$padding-tiny;
}

.tools-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(30px, 1fr));
    gap: vars.$gap-small;
}

.tool-button {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 100%;
    aspect-ratio: 1 / 1;
    padding: vars.$padding-small;
    background-color: color.adjust(vars.$color-dark-blue-2, $lightness: -5%);
    color: vars.$color-white;
    border: 1px solid vars.$color-accent-blue;
    border-radius: vars.$border-radius;
    cursor: pointer;
    transition: background-color 0.1s ease-in-out, border-color 0.1s ease-in-out;

    &:hover {
        background-color: color.adjust(vars.$color-dark-blue-2, $lightness: 5%);
        border-color: color.adjust(vars.$color-accent-blue, $lightness: 10%);
    }

    &.active-tool {
        background-color: vars.$color-primary;
        color: vars.$color-white;
        border-color: color.adjust(vars.$color-primary, $lightness: -10%);
    }

    .tool-icon {
        font-size: vars.$font_size_large;
    }
}

.labels-list {
    display: flex;
    flex-direction: column;
    gap: vars.$gap-small;
}

.label-button {
    display: flex;
    align-items: center;
    width: 100%;
    padding: calc(vars.$padding-small * 0.75) vars.$padding-small;
    background-color: color.adjust(vars.$color-dark-blue-2, $lightness: -3%);
    color: vars.$color-white;
    border: 1px solid vars.$color-accent-blue;
    border-radius: vars.$border-radius;
    cursor: pointer;
    text-align: left;
    font-size: vars.$font_size_small;
    transition: background-color 0.1s ease-in-out, border-color 0.1s ease-in-out;

    &:hover {
        background-color: color.adjust(vars.$color-dark-blue-2, $lightness: 5%);
        border-color: color.adjust(vars.$color-accent-blue, $lightness: 10%);
    }

    &.active-label {
        background-color: vars.$color-primary;
        color: vars.$color-white;
        border-color: color.adjust(vars.$color-primary, $lightness: -10%);
        font-weight: vars.$font-weight-heading;

        .label-color-swatch {
            border-color: rgba(vars.$color-white, 0.7);
        }
    }
}

.label-color-swatch {
    width: 12px;
    height: 12px;
    border-radius: 3px;
    margin-right: vars.$margin-small;
    border: 1px solid vars.$color-accent-blue;
    flex-shrink: 0;
}

.label-name {
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}

.no-labels-message {
    font-size: vars.$font_size_small;
    color: color.adjust(vars.$color-white, $lightness: 20%);
    padding: vars.$padding-small;
    text-align: center;
}
</style>