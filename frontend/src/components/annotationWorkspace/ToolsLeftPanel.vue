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
@use "@/styles/variables" as vars;
@use "@/styles/mixins" as mixins;

.tools-panel-left {
    padding: vars.$padding-small;
    background-color: vars.$ws-panel-bg;
    color: vars.$ws-panel-text;
    overflow-y: auto;
    @include mixins.flex-column($gap: vars.$padding-medium);
}

.panel-section {
    @include mixins.flex-column($gap: vars.$padding-small);
}

.section-title {
    font-size: vars.$font_size_small;
    font-weight: vars.$font-weight-heading;
    color: vars.$ws-panel-text;
    text-transform: uppercase;
    margin-bottom: vars.$padding-smallest;
    border-bottom: 1px solid color.adjust(vars.$ws-border, $lightness: 10%);
    padding-bottom: vars.$padding-smallest;
}

.tools-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(30px, 1fr));
    gap: vars.$padding-small;
}

.tool-button {
    width: 100%;
    aspect-ratio: 1 / 1;
    padding: vars.$padding-small;
    background-color: color.adjust(vars.$ws-panel-bg, $lightness: -5%);
    color: vars.$ws-panel-text;
    border: 1px solid vars.$ws-border;
    border-radius: vars.$border-radius-standard;
    cursor: pointer;
    transition: background-color vars.$transition-fast-ease-in-out, border-color vars.$transition-fast-ease-in-out;
    @include mixins.flex-center;

    &:hover {
        background-color: color.adjust(vars.$ws-panel-bg, $lightness: 5%);
        border-color: color.adjust(vars.$ws-border, $lightness: 10%);
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
    @include mixins.flex-column($gap: calc(vars.$padding-small / 2));
}

.label-button {
    width: 100%;
    padding: calc(vars.$padding-small * 0.75) vars.$padding-small;
    background-color: color.adjust(vars.$ws-panel-bg, $lightness: -3%);
    color: vars.$ws-panel-text;
    border: 1px solid vars.$ws-border;
    border-radius: vars.$border-radius-standard;
    cursor: pointer;
    text-align: left;
    font-size: vars.$font_size_small;
    transition: background-color vars.$transition-fast-ease-in-out, border-color vars.$transition-fast-ease-in-out;
    @include mixins.flexbox($align-items: center);

    &:hover {
        background-color: color.adjust(vars.$ws-panel-bg, $lightness: 5%);
        border-color: color.adjust(vars.$ws-border, $lightness: 10%);
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
    margin-right: vars.$padding-small;
    border: 1px solid vars.$ws-border;
    flex-shrink: 0;
}

.label-name {
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}

.no-labels-message {
    font-size: vars.$font_size_small;
    color: color.adjust(vars.$ws-panel-text, $lightness: 20%);
    padding: vars.$padding-small;
    text-align: center;
}
</style>