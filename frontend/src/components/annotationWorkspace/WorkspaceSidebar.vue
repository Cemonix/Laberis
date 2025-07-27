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

        <!-- Label Scheme Selector -->
        <div class="panel-section">
            <h3 class="section-title">Labels</h3>
            <div v-if="availableLabelSchemes.length > 1" class="scheme-selector">
                <label for="scheme-dropdown" class="selector-label">Label Scheme:</label>
                <select 
                    id="scheme-dropdown"
                    v-model="selectedSchemeId" 
                    @change="onSchemeChange"
                    class="scheme-dropdown"
                >
                    <option 
                        v-for="scheme in availableLabelSchemes" 
                        :key="scheme.labelSchemeId"
                        :value="scheme.labelSchemeId"
                    >
                        {{ scheme.name }}
                    </option>
                </select>
            </div>

            <!-- Loading state -->
            <div v-if="isLoading" class="panel-loading">
                <span>Loading labels...</span>
            </div>

            <!-- No labels state -->
            <div v-else-if="!availableLabels || availableLabels.length === 0" class="no-labels">
                <p>No labels available</p>
                <small>Create a label scheme for this project to start annotating.</small>
            </div>

            <!-- Search box for labels -->
            <div v-if="availableLabels.length > 5" class="label-search">
                <input 
                    v-model="labelSearchQuery"
                    type="text" 
                    placeholder="Search labels..."
                    class="search-input"
                    @keydown.escape="labelSearchQuery = ''"
                />
                <div class="search-results-count" v-if="labelSearchQuery">
                    {{ filteredLabels.length }} of {{ availableLabels.length }}
                </div>
            </div>

            <!-- Labels list -->
            <div v-if="availableLabels.length > 0" class="labels-list">
                <div 
                    v-for="(label, index) in filteredLabels" 
                    :key="label.labelId"
                    :class="['label-item', { 'label-selected': label.labelId === selectedLabelId }]"
                    @click="selectLabel(label.labelId)"
                    @contextmenu.prevent="showLabelContextMenu($event, label)"
                    :title="getLabelTooltip(label, index)"
                    :data-index="index"
                >
                    <div class="label-header">
                        <div 
                            class="label-color-indicator" 
                            :style="{ backgroundColor: label.color }"
                        ></div>
                        <span class="label-name">{{ label.name }}</span>
                        <span class="label-shortcut" v-if="index < 9">{{ index + 1 }}</span>
                    </div>
                    <small v-if="label.description" class="label-description">{{ label.description }}</small>
                    <div class="label-stats" v-if="getLabelUsageCount(label.labelId) > 0">
                        <small class="usage-count">{{ getLabelUsageCount(label.labelId) }} annotations</small>
                    </div>
                </div>
            </div>

            <!-- Keyboard shortcuts help -->
            <div v-if="availableLabels.length > 0" class="shortcuts-help">
                <details>
                    <summary class="shortcuts-toggle">Keyboard Shortcuts</summary>
                    <div class="shortcuts-content">
                        <div class="shortcut-item">
                            <kbd>1-9</kbd>
                            <span>Select label</span>
                        </div>
                        <div class="shortcut-item">
                            <kbd>Ctrl+L</kbd>
                            <span>Search labels</span>
                        </div>
                        <div class="shortcut-item">
                            <kbd>Esc</kbd>
                            <span>Clear search</span>
                        </div>
                    </div>
                </details>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed, ref, watch, onMounted, onUnmounted } from 'vue';
import { useWorkspaceStore } from '@/stores/workspaceStore';
import type { ToolName } from '@/types/workspace/tools';
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';
import Button from '@/components/common/Button.vue';
import { AppLogger } from '@/utils/logger';

const logger = AppLogger.createComponentLogger('WorkspaceSidebar');

const workspaceStore = useWorkspaceStore();

const availableTools = computed(() => workspaceStore.availableTools);
const activeToolId = computed(() => workspaceStore.activeTool);
const isLoading = computed(() => workspaceStore.getLoadingState);
const availableLabels = computed(() => workspaceStore.getAvailableLabels);
const selectedLabelId = computed(() => workspaceStore.getSelectedLabelId);
const currentLabelScheme = computed(() => workspaceStore.getCurrentLabelScheme);
const availableLabelSchemes = computed(() => workspaceStore.getAvailableLabelSchemes);
const annotations = computed(() => workspaceStore.getAnnotations);

// Track selected scheme ID and search functionality
const selectedSchemeId = ref<number | null>(null);
const labelSearchQuery = ref('');

// Filtered labels based on search query
const filteredLabels = computed(() => {
    if (!labelSearchQuery.value.trim()) {
        return availableLabels.value;
    }
    
    const query = labelSearchQuery.value.toLowerCase().trim();
    return availableLabels.value.filter(label => 
        label.name.toLowerCase().includes(query) ||
        (label.description && label.description.toLowerCase().includes(query))
    );
});

// Initialize selectedSchemeId when currentLabelScheme changes
watch(currentLabelScheme, (newScheme) => {
    if (newScheme) {
        selectedSchemeId.value = newScheme.labelSchemeId;
    }
}, { immediate: true });

const selectTool = (toolId: ToolName) => {
    workspaceStore.setActiveTool(toolId);
};

const selectLabel = (labelId: number) => {
    if (selectedLabelId.value === labelId) {
        workspaceStore.setCurrentLabelId(null);
    } else {
        workspaceStore.setCurrentLabelId(labelId);
    }
};

const onSchemeChange = async () => {
    if (selectedSchemeId.value) {
        await workspaceStore.switchLabelScheme(selectedSchemeId.value);
    }
};

// Helper functions
const getLabelUsageCount = (labelId: number): number => {
    return annotations.value.filter(annotation => annotation.labelId === labelId).length;
};

const getLabelTooltip = (label: any, index: number): string => {
    const usageCount = getLabelUsageCount(label.labelId);
    const shortcut = index < 9 ? ` (Press ${index + 1})` : '';
    const usage = usageCount > 0 ? ` • ${usageCount} annotations` : '';
    return `${label.name}${shortcut}${usage}${label.description ? ` • ${label.description}` : ''}`;
};

const showLabelContextMenu = (_event: MouseEvent, label: any) => {
    // TODO: Context menu functionality can be expanded later
    logger.info('Context menu for label:', label.name);
};

// Keyboard shortcuts for label selection
const handleKeydown = (event: KeyboardEvent) => {
    // Only handle shortcuts when not typing in an input
    if (event.target instanceof HTMLInputElement || event.target instanceof HTMLTextAreaElement) {
        return;
    }

    // Handle number keys 1-9 for quick label selection
    if (event.key >= '1' && event.key <= '9' && !event.ctrlKey && !event.altKey && !event.metaKey) {
        event.preventDefault();
        const labelIndex = parseInt(event.key) - 1;
        const targetLabel = filteredLabels.value[labelIndex];
        
        if (targetLabel) {
            selectLabel(targetLabel.labelId);
        }
    }
    
    // Handle 'L' key to focus search input
    if (event.key.toLowerCase() === 'l' && event.ctrlKey && !event.altKey && !event.metaKey) {
        event.preventDefault();
        const searchInput = document.querySelector('.search-input') as HTMLInputElement;
        if (searchInput) {
            searchInput.focus();
        }
    }
    
    // Handle Escape to clear search
    if (event.key === 'Escape' && labelSearchQuery.value) {
        labelSearchQuery.value = '';
    }
};

// Lifecycle hooks for keyboard shortcuts
onMounted(() => {
    document.addEventListener('keydown', handleKeydown);
});

onUnmounted(() => {
    document.removeEventListener('keydown', handleKeydown);
});

</script>

<style scoped>
.tools-panel-left {
    display: flex;
    flex-direction: column;
    gap: 1rem;
    padding: 1rem;
    background-color: var(--color-dark-blue-2);
    color: var(--color-white);
    overflow-y: auto;
    height: 100%;
}

.panel-section {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    margin-bottom: 1rem;
}

.section-title {
    font-size: 1.1rem;
    font-weight: 700;
    color: var(--color-white);
    text-transform: uppercase;
    margin-bottom: 0.5rem;
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
}
.tool-button:hover {
    background-color: var(--color-dark-blue-3);
    border-color: var(--color-accent-blue);
}
.tool-button.active-tool {
    background-color: var(--color-primary);
    color: var(--color-white);
    border-color: var(--color-primary);
}
.tool-icon {
    font-size: 1.25rem;
}

.scheme-selector {
    margin-bottom: 1rem;
    padding: 0.75rem;
    background-color: var(--color-dark-blue-3);
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
}
.scheme-dropdown:focus {
    border-color: var(--color-primary);
}
.scheme-dropdown:hover {
    border-color: var(--color-accent-blue);
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
}
.no-labels p {
    margin-bottom: 0.5rem;
    font-weight: bold;
}
.no-labels small {
    color: var(--color-gray-500);
    line-height: 1.4;
}
.labels-list {
    flex: 1;
    overflow-y: auto;
    margin-bottom: 1rem;
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
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
}
.label-item:hover {
    background-color: rgba(var(--color-accent-blue), 0.2);
    border-color: var(--color-accent-blue);
}
.label-item.label-selected {
    border-color: var(--color-primary);
    background-color: rgba(var(--color-primary), 0.1);
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

/* Enhanced Label Styles */
.label-search {
    margin-bottom: 1rem;
    position: relative;
}

.search-input {
    width: 100%;
    padding: 0.5rem;
    background-color: var(--color-dark-blue-3);
    color: var(--color-gray-200);
    border: 1px solid var(--color-gray-600);
    border-radius: 4px;
    font-size: 0.875rem;
    outline: none;
    transition: border-color 0.2s ease-in-out;
}

.search-input:focus {
    border-color: var(--color-primary);
}

.search-input::placeholder {
    color: var(--color-gray-500);
}

.search-results-count {
    position: absolute;
    top: 100%;
    right: 0;
    margin-top: 0.25rem;
    font-size: 0.75rem;
    color: var(--color-gray-400);
    background-color: var(--color-dark-blue-3);
    padding: 0.125rem 0.25rem;
    border-radius: 2px;
}

.label-header {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    margin-bottom: 0.25rem;
}

.label-shortcut {
    margin-left: auto;
    background-color: var(--color-dark-blue-1);
    color: var(--color-gray-300);
    font-size: 0.75rem;
    font-weight: bold;
    padding: 0.125rem 0.375rem;
    border-radius: 3px;
    border: 1px solid var(--color-gray-600);
    min-width: 1.5rem;
    text-align: center;
    flex-shrink: 0;
}

.label-item:hover .label-shortcut {
    background-color: var(--color-accent-blue);
    color: var(--color-white);
    border-color: var(--color-accent-blue);
}

.label-item.label-selected .label-shortcut {
    background-color: var(--color-primary);
    color: var(--color-white);
    border-color: var(--color-primary);
}

.label-stats {
    margin-top: 0.25rem;
    padding-top: 0.25rem;
    border-top: 1px solid var(--color-gray-600);
}

.usage-count {
    color: var(--color-gray-500);
    font-size: 0.75rem;
    font-style: italic;
}

.label-item:hover .usage-count {
    color: var(--color-gray-400);
}

.label-item.label-selected .usage-count {
    color: var(--color-gray-300);
}

/* Enhanced color indicator */
.label-color-indicator {
    box-shadow: 0 0 0 2px var(--color-dark-blue-3);
    transition: transform 0.2s ease-in-out, box-shadow 0.2s ease-in-out;
}

.label-item:hover .label-color-indicator {
    transform: scale(1.1);
    box-shadow: 0 0 0 2px var(--color-accent-blue);
}

.label-item.label-selected .label-color-indicator {
    transform: scale(1.15);
    box-shadow: 0 0 0 2px var(--color-primary);
}

/* Improve accessibility and visual feedback */
.label-item {
    position: relative;
    user-select: none;
}

.label-item:focus-within {
    outline: 2px solid var(--color-primary);
    outline-offset: 2px;
}

/* Animation for smooth interactions */
.labels-list {
    animation: fadeIn 0.3s ease-in-out;
}

@keyframes fadeIn {
    from {
        opacity: 0;
        transform: translateY(-10px);
    }
    to {
        opacity: 1;
        transform: translateY(0);
    }
}

/* Keyboard shortcuts help */
.shortcuts-help {
    margin-top: 1rem;
    border-top: 1px solid var(--color-gray-600);
    padding-top: 0.75rem;
}

.shortcuts-toggle {
    color: var(--color-gray-300);
    font-size: 0.875rem;
    font-weight: bold;
    cursor: pointer;
    list-style: none;
    padding: 0.5rem;
    background-color: var(--color-dark-blue-3);
    border-radius: 4px;
    border: 1px solid var(--color-gray-600);
    transition: all 0.2s ease-in-out;
}

.shortcuts-toggle:hover {
    background-color: var(--color-dark-blue-1);
    border-color: var(--color-accent-blue);
}

.shortcuts-toggle::-webkit-details-marker {
    display: none;
}

.shortcuts-toggle::before {
    content: '▶';
    display: inline-block;
    margin-right: 0.5rem;
    transition: transform 0.2s ease-in-out;
}

details[open] .shortcuts-toggle::before {
    transform: rotate(90deg);
}

.shortcuts-content {
    margin-top: 0.5rem;
    padding: 0.75rem;
    background-color: var(--color-dark-blue-3);
    border-radius: 4px;
    border: 1px solid var(--color-gray-600);
}

.shortcut-item {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 0.5rem;
    font-size: 0.75rem;
}

.shortcut-item:last-child {
    margin-bottom: 0;
}

.shortcut-item kbd {
    background-color: var(--color-dark-blue-1);
    color: var(--color-gray-200);
    padding: 0.125rem 0.375rem;
    border-radius: 3px;
    font-family: monospace;
    font-size: 0.75rem;
    border: 1px solid var(--color-gray-600);
    box-shadow: 0 1px 0 var(--color-gray-600);
}

.shortcut-item span {
    color: var(--color-gray-400);
}
</style>