<template>
    <ModalWindow title="Add Widget" :isOpen="true" @close="$emit('close')" size="medium">
        <div class="add-widget-modal">
            <div class="widget-categories">
                <div class="search-section">
                    <input
                        v-model="searchQuery"
                        type="text"
                        placeholder="Search widgets..."
                        class="widget-search"
                    />
                </div>

                <div class="category-filters">
                    <button
                        v-for="category in categories"
                        :key="category"
                        :class="{
                            'category-btn': true,
                            'category-btn-active':
                                selectedCategory === category,
                        }"
                        @click="selectedCategory = category"
                    >
                        {{ category }}
                    </button>
                </div>
            </div>

            <div class="widget-grid">
                <div
                    v-for="widget in filteredWidgets"
                    :key="widget.widgetType"
                    :class="{
                        'widget-card': true,
                        'widget-card-selected':
                            selectedWidgetTypes.includes(widget.widgetType),
                    }"
                    @click="toggleWidget(widget)"
                >
                    <div class="widget-checkbox">
                        <input
                            type="checkbox"
                            :checked="selectedWidgetTypes.includes(widget.widgetType)"
                            @click.stop
                            @change="toggleWidget(widget)"
                        />
                    </div>
                    <div class="widget-icon">
                        <font-awesome-icon :icon="getWidgetIcon(widget.widgetType)" />
                    </div>
                    <div class="widget-info">
                        <h3 class="widget-name">{{ widget.title }}</h3>
                        <p class="widget-description">
                            {{ widget.description }}
                        </p>
                        <div class="widget-meta">
                            <span class="widget-size"
                                >{{ widget.defaultWidth }}×{{
                                    widget.defaultHeight
                                }}</span
                            >
                            <span
                                v-if="widget.requiresConfiguration"
                                class="widget-config"
                            >
                                <font-awesome-icon :icon="faCog" /> Configurable
                            </span>
                        </div>
                    </div>
                </div>
            </div>

            <div v-if="selectedWidgetTypes.length > 0" class="widget-preview">
                <h4>Selected Widgets ({{ selectedWidgetTypes.length }})</h4>
                <div class="selected-widgets-list">
                    <div
                        v-for="widgetType in selectedWidgetTypes"
                        :key="widgetType"
                        class="selected-widget-item"
                    >
                        <div class="selected-widget-info">
                            <div class="selected-widget-icon">
                                <font-awesome-icon :icon="getWidgetIcon(widgetType)" />
                            </div>
                            <div class="selected-widget-details">
                                <span class="selected-widget-name">
                                    {{ getWidgetDefinition(widgetType)?.title }}
                                </span>
                                <span class="selected-widget-size">
                                    {{ getWidgetDefinition(widgetType)?.defaultWidth }}×{{ getWidgetDefinition(widgetType)?.defaultHeight }}
                                </span>
                            </div>
                        </div>
                        <button
                            @click="removeWidget(widgetType)"
                            class="remove-widget-btn"
                            type="button"
                        >
                            <font-awesome-icon :icon="faTimes" />
                        </button>
                    </div>
                </div>
            </div>

            <div v-if="filteredWidgets.length === 0" class="no-widgets">
                <font-awesome-icon :icon="faSearch" />
                <p>No widgets found matching your search.</p>
            </div>
        </div>

        <template #footer>
            <div class="modal-actions">
                <Button @click="$emit('close')" variant="secondary">
                    Cancel
                </Button>
                <Button
                    @click="addWidget"
                    :disabled="selectedWidgetTypes.length === 0"
                    variant="primary"
                >
                    Add {{ selectedWidgetTypes.length > 1 ? `${selectedWidgetTypes.length} Widgets` : 'Widget' }}
                </Button>
            </div>
        </template>
    </ModalWindow>
</template>

<script setup lang="ts">
import { computed, ref } from "vue";
import { FontAwesomeIcon } from "@fortawesome/vue-fontawesome";
import { 
    faHeart, 
    faProjectDiagram, 
    faUser, 
    faChartBar, 
    faHistory, 
    faUsers, 
    faShieldAlt, 
    faDatabase, 
    faCube, 
    faCog, 
    faSearch,
    faTimes
} from "@fortawesome/free-solid-svg-icons";
import ModalWindow from "@/components/common/modal/ModalWindow.vue";
import Button from "@/components/common/Button.vue";
import type { WidgetDefinitionDto } from "@/services/project/dashboard/dashboard.types";

interface Props {
    widgetDefinitions: WidgetDefinitionDto[];
}

interface Emits {
    (e: "add", payload: { widgetTypes: string[] }): void;
    (e: "close"): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

// Reactive state
const searchQuery = ref("");
const selectedCategory = ref("All");
const selectedWidgetTypes = ref<string[]>([]);

// Computed properties
const categories = computed(() => {
    const cats = new Set<string>(["All"]);

    // Categorize widgets based on their type
    props.widgetDefinitions.forEach((widget) => {
        if (
            widget.widgetType.includes("health") ||
            widget.widgetType.includes("overview")
        ) {
            cats.add("Overview");
        } else if (
            widget.widgetType.includes("workflow") ||
            widget.widgetType.includes("progress")
        ) {
            cats.add("Progress");
        } else if (
            widget.widgetType.includes("user") ||
            widget.widgetType.includes("performance")
        ) {
            cats.add("Performance");
        } else if (
            widget.widgetType.includes("task") ||
            widget.widgetType.includes("statistics")
        ) {
            cats.add("Analytics");
        } else if (
            widget.widgetType.includes("activity") ||
            widget.widgetType.includes("recent")
        ) {
            cats.add("Activity");
        } else {
            cats.add("Other");
        }
    });

    return Array.from(cats);
});

const filteredWidgets = computed(() => {
    let widgets = props.widgetDefinitions;

    // Filter by search query
    if (searchQuery.value) {
        const query = searchQuery.value.toLowerCase();
        widgets = widgets.filter(
            (widget) =>
                widget.title.toLowerCase().includes(query) ||
                widget.description.toLowerCase().includes(query) ||
                widget.widgetType.toLowerCase().includes(query)
        );
    }

    // Filter by category
    if (selectedCategory.value !== "All") {
        widgets = widgets.filter((widget) => {
            const category = selectedCategory.value.toLowerCase();
            return (
                widget.widgetType.toLowerCase().includes(category) ||
                widget.title.toLowerCase().includes(category) ||
                widget.description.toLowerCase().includes(category)
            );
        });
    }

    return widgets;
});

// Methods
const toggleWidget = (widget: WidgetDefinitionDto) => {
    const index = selectedWidgetTypes.value.indexOf(widget.widgetType);
    if (index > -1) {
        selectedWidgetTypes.value.splice(index, 1);
    } else {
        selectedWidgetTypes.value.push(widget.widgetType);
    }
};

const removeWidget = (widgetType: string) => {
    const index = selectedWidgetTypes.value.indexOf(widgetType);
    if (index > -1) {
        selectedWidgetTypes.value.splice(index, 1);
    }
};

const getWidgetDefinition = (widgetType: string) => {
    return props.widgetDefinitions.find(w => w.widgetType === widgetType);
};

const getWidgetIcon = (widgetType: string) => {
    const iconMap: Record<string, any> = {
        project_health: faHeart,
        workflow_progress: faProjectDiagram,
        user_performance: faUser,
        task_statistics: faChartBar,
        recent_activities: faHistory,
        team_productivity: faUsers,
        quality_metrics: faShieldAlt,
        data_overview: faDatabase,
    };

    return iconMap[widgetType] || faCube;
};

const addWidget = () => {
    if (selectedWidgetTypes.value.length === 0) return;

    emit("add", {
        widgetTypes: [...selectedWidgetTypes.value],
    });
};
</script>

<style scoped>
.add-widget-modal {
    display: flex;
    flex-direction: column;
    gap: 1.5rem;
    max-height: 70vh;
    padding: 1rem;
}

.widget-categories {
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.search-section {
    display: flex;
}

.widget-search {
    flex: 1;
    padding: 1rem;
    border: 0.125rem solid var(--color-gray-300);
    border-radius: 0.75rem;
    font-size: 0.875rem;
    background: var(--color-gray-100);
    transition: all 0.2s ease;
}

.widget-search:focus {
    outline: none;
    border-color: var(--color-primary);
    box-shadow: 0 0 0 0.1875rem rgba(0, 123, 255, 0.15);
    background: var(--color-white);
    transform: translateY(-0.0625rem);
}

.category-filters {
    display: flex;
    gap: 0.5rem;
    flex-wrap: wrap;
}

.category-btn {
    padding: 0.75rem 1rem;
    background: var(--color-gray-100);
    border: 0.125rem solid var(--color-gray-300);
    border-radius: 1.5rem;
    cursor: pointer;
    font-size: 0.8125rem;
    font-weight: 500;
    transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
    white-space: nowrap;
}

.category-btn:hover {
    background: var(--color-blue-100);
    border-color: var(--color-primary);
    transform: translateY(-0.0625rem);
    box-shadow: 0 0.25rem 0.75rem rgba(0, 123, 255, 0.15);
}

.category-btn-active {
    background: var(--color-primary);
    color: var(--color-white);
    border-color: var(--color-primary);
    transform: translateY(-0.0625rem);
    box-shadow: 0 0.25rem 0.75rem rgba(0, 123, 255, 0.25);
}

.widget-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(20rem, 1fr));
    gap: 1rem;
    max-height: 20rem;
    padding: 0.75rem;
    border-radius: 0.75rem;
    background: var(--color-gray-50);
    overflow-y: auto;
    z-index: 1;
}

.widget-card {
    display: flex;
    gap: 1rem;
    padding: 1.5rem;
    background: var(--color-white);
    border: 0.125rem solid var(--color-gray-300);
    border-radius: 1rem;
    cursor: pointer;
    transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
    position: relative;
}

.widget-card::before {
    content: '';
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background: linear-gradient(135deg, transparent 0%, rgba(0, 123, 255, 0.05) 100%);
    opacity: 0;
    transition: opacity 0.3s ease;
    pointer-events: none;
}

.widget-card:hover {
    border-color: var(--color-primary);
    box-shadow: 0 0.5rem 1.5rem rgba(0, 0, 0, 0.12);
    transform: translateY(-0.125rem);
}

.widget-card:hover::before {
    opacity: 1;
}

.widget-card-selected {
    border-color: var(--color-primary);
    background: var(--color-blue-100);
    box-shadow: 0 0 0 0.1875rem rgba(0, 123, 255, 0.2), 
                0 0.5rem 1.5rem rgba(0, 123, 255, 0.15);
    transform: translateY(-0.125rem);
}

.widget-icon {
    flex-shrink: 0;
    display: flex;
    align-items: center;
    justify-content: center;
    width: 3rem;
    height: 3rem;
    background: linear-gradient(135deg, var(--color-blue-100) 0%, var(--color-gray-200) 100%);
    border-radius: 0.75rem;
    color: var(--color-primary);
    font-size: 1.25rem;
    position: relative;
    z-index: 1;
}

.widget-info {
    flex: 1;
    min-width: 0;
}

.widget-name {
    margin: 0 0 0.5rem 0;
    font-size: 0.875rem;
    font-weight: 600;
    color: var(--color-gray-800);
}

.widget-description {
    margin: 0 0 0.5rem 0;
    font-size: 0.75rem;
    color: var(--color-gray-600);
    line-height: 1.4;
    display: -webkit-box;
    line-clamp: 2;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
}

.widget-meta {
    display: flex;
    gap: 0.75rem;
    align-items: center;
}

.widget-size {
    font-size: 0.6875rem;
    color: var(--color-gray-600);
    background: var(--color-gray-100);
    padding: 0.125rem 0.375rem;
    border-radius: 0.25rem;
}

.widget-config {
    font-size: 0.6875rem;
    color: var(--color-secondary);
    display: flex;
    align-items: center;
    gap: 0.125rem;
}

.widget-checkbox {
    position: absolute;
    top: 0.75rem;
    right: 0.75rem;
    z-index: 10;
}

.widget-checkbox input[type="checkbox"] {
    width: 1.125rem;
    height: 1.125rem;
    accent-color: var(--color-primary);
    cursor: pointer;
}

.widget-preview {
    padding: 1rem;
    background: var(--color-gray-100);
    border-radius: 0.5rem;
    position: relative;
    z-index: 2;
}

.selected-widgets-list {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
    max-height: 12rem;
    overflow-y: auto;
}

.selected-widget-item {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 0.75rem;
    background: var(--color-white);
    border: 1px solid var(--color-gray-300);
    border-radius: 0.5rem;
}

.selected-widget-info {
    display: flex;
    align-items: center;
    gap: 0.75rem;
}

.selected-widget-icon {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 2rem;
    height: 2rem;
    background: linear-gradient(135deg, var(--color-blue-100) 0%, var(--color-gray-200) 100%);
    border-radius: 0.5rem;
    color: var(--color-primary);
    font-size: 1rem;
}

.selected-widget-details {
    display: flex;
    flex-direction: column;
    gap: 0.125rem;
}

.selected-widget-name {
    font-size: 0.875rem;
    font-weight: 500;
    color: var(--color-gray-800);
}

.selected-widget-size {
    font-size: 0.75rem;
    color: var(--color-gray-600);
}

.remove-widget-btn {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 1.5rem;
    height: 1.5rem;
    background: var(--color-gray-200);
    border: none;
    border-radius: 0.25rem;
    color: var(--color-gray-600);
    cursor: pointer;
    transition: all 0.2s ease;
}

.remove-widget-btn:hover {
    background: var(--color-error);
    color: var(--color-white);
}

.widget-preview h4 {
    margin: 0 0 1rem 0;
    font-size: 0.875rem;
    font-weight: 600;
}

.no-widgets {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 2rem;
    color: var(--color-gray-600);
    text-align: center;
}

.no-widgets i {
    font-size: 2rem;
    margin-bottom: 1rem;
    opacity: 0.5;
}

.modal-actions {
    display: flex;
    gap: 1.5rem;
    justify-content: flex-end;
    padding: 1.5rem 0;
}

.modal-actions :deep(button) {
    padding: 1rem 2rem !important;
    min-height: 2.75rem !important;
    border-radius: 0.75rem !important;
    font-weight: 500 !important;
    transition: all 0.2s ease !important;
}

.modal-actions :deep(button:hover) {
    transform: translateY(-0.0625rem) !important;
    box-shadow: 0 0.25rem 0.75rem rgba(0, 0, 0, 0.15) !important;
}


/* Responsive Design */
@media (max-width: 768px) {
    .widget-grid {
        grid-template-columns: 1fr;
        max-height: 15.625rem;
    }

    .widget-card {
        padding: 0.75rem;
        padding-right: 2.5rem; /* Make room for checkbox */
    }

    .selected-widgets-list {
        max-height: 8rem;
    }

    .selected-widget-item {
        padding: 0.5rem;
    }

    .selected-widget-icon {
        width: 1.5rem;
        height: 1.5rem;
        font-size: 0.875rem;
    }
}
</style>
