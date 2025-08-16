<template>
    <div 
        class="grid-stack-item" 
        :gs-id="gsId"
        :gs-x="gsX"
        :gs-y="gsY"
        :gs-w="gsW"
        :gs-h="gsH"
        @click="$emit('select')"
    >
        <div 
            class="widget-container grid-stack-item-content"
            :class="{ 'widget-selected': selected }"
        >
            <div class="widget-header">
                <div class="widget-title-section">
                    <h3 class="widget-title">{{ widget.title }}</h3>
                    <div class="widget-subtitle" v-if="subtitle">
                        {{ subtitle }}
                    </div>
                </div>
                <div class="widget-actions">
                    <button
                        class="widget-action-btn"
                        @click.stop="$emit('settings')"
                        title="Settings"
                    >
                        <font-awesome-icon :icon="faCog" />
                    </button>
                    <button
                        class="widget-action-btn"
                        @click.stop="$emit('refresh')"
                        :disabled="refreshing"
                        title="Refresh"
                    >
                        <font-awesome-icon
                            :icon="faRefresh"
                            :class="{ rotating: refreshing }"
                        />
                    </button>
                    <button
                        class="widget-action-btn widget-remove"
                        @click.stop="$emit('remove')"
                        title="Remove widget"
                    >
                        <font-awesome-icon :icon="faTimes" />
                    </button>
                </div>
            </div>

            <div class="widget-content" :class="{ 'widget-loading': loading }">
                <div v-if="loading" class="widget-loader">
                    <div class="spinner"></div>
                    <span>Loading...</span>
                </div>
                <div v-else-if="error" class="widget-error">
                    <font-awesome-icon :icon="faExclamationTriangle" />
                    <span>{{ error }}</span>
                    <Button @click="$emit('retry')" variant="primary" class="widget-retry-btn">
                        Retry
                    </Button>
                </div>
                <div v-else class="widget-body">
                    <slot></slot>
                </div>
            </div>

            </div>
    </div>
</template>

<script setup lang="ts">
import { FontAwesomeIcon } from "@fortawesome/vue-fontawesome";
import { faCog, faRefresh, faTimes, faExclamationTriangle } from "@fortawesome/free-solid-svg-icons";
import type { WidgetInstanceDto } from "@/types/dashboard/dashboard";
import Button from "@/components/common/Button.vue";

interface Props {
    widget: WidgetInstanceDto;
    loading?: boolean;
    error?: string | null;
    subtitle?: string;
    selected?: boolean;
    refreshing?: boolean;
    gsId?: string;
    gsX?: number;
    gsY?: number;
    gsW?: number;
    gsH?: number;
}

interface Emits {
    (e: "select"): void;
    (e: "settings"): void;
    (e: "refresh"): void;
    (e: "remove"): void;
    (e: "retry"): void;
}

defineProps<Props>();
defineEmits<Emits>();
</script>

<style scoped>
.widget-container {
    background: var(--color-white);
    border: 1px solid var(--color-gray-300);
    border-radius: 0.75rem;
    overflow: hidden; /* Changed from auto to hidden for cleaner resizing */
    transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
    box-shadow: 0 0.125rem 0.5rem rgba(0, 0, 0, 0.08);
}

.widget-container:hover {
    border-color: var(--color-blue-200);
    box-shadow: 0 0.5rem 1.5rem rgba(0, 0, 0, 0.12);
}

.widget-selected {
    border-color: var(--color-primary);
    box-shadow: 0 0 0 0.1875rem rgba(0, 123, 255, 0.25), 
        0 0.5rem 1.5rem rgba(0, 0, 0, 0.15);
}

.widget-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 0.875rem 1.125rem;
    background: linear-gradient(135deg, var(--color-gray-100) 0%, var(--color-gray-200) 100%);
    border-bottom: 1px solid var(--color-gray-300);
    cursor: grab;
    user-select: none;
    transition: background 0.2s ease;
}

.widget-header:active {
    cursor: grabbing;
}

.widget-title-section {
    flex: 1;
    min-width: 0;
}

.widget-title {
    margin: 0;
    font-size: 0.875rem;
    font-weight: 600;
    color: var(--color-gray-800);
    line-height: 1.2;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.widget-subtitle {
    font-size: 0.75rem;
    color: var(--color-gray-600);
    margin-top: 0.125rem;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.widget-actions {
    display: flex;
    gap: 0.375rem;
    margin-left: 0.75rem;
    align-items: center;
}

.widget-action-btn {
    background: rgba(33, 37, 41, 0.05) !important;
    border: none !important;
    padding: 0.5rem !important;
    border-radius: 0.5rem !important;
    cursor: pointer !important;
    color: var(--color-gray-600) !important;
    transition: all 0.2s ease !important;
    font-size: 0.875rem !important;
    display: flex !important;
    align-items: center !important;
    justify-content: center !important;
    min-width: 2rem !important;
    min-height: 2rem !important;
    margin: 0 0.125rem !important;
}

.widget-action-btn:hover {
    background: rgba(33, 37, 41, 0.1) !important;
    color: var(--color-gray-800) !important;
    transform: scale(1.05) !important;
}

.widget-action-btn:disabled {
    opacity: 0.5;
    cursor: not-allowed;
}

.widget-remove {
    color: var(--color-error);
}

.widget-remove:hover {
    background: var(--color-red-100);
    color: var(--color-red-800);
}

/* Content */
.widget-content {
    flex: 1;
    display: flex;
    flex-direction: column;
    min-height: 0; /* Important for flexbox children to scroll correctly */
    position: relative;
    overflow-y: auto; /* Allow content to scroll if it overflows */
}

.widget-body {
    flex: 1;
    padding: 1rem;
    overflow: visible;
}

.widget-loading {
    background: var(--color-gray-100);
}

.widget-loader, .widget-error {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    height: 100%;
    color: var(--color-gray-600);
    gap: 0.5rem;
    padding: 1rem;
    text-align: center;
}

.widget-error {
    color: var(--color-error);
}

.widget-retry-btn {
    margin-top: 0.5rem;
    font-size: 0.75rem;
}

/* Animations */
.spinner {
    width: 1.25rem;
    height: 1.25rem;
    border: 0.125rem solid var(--color-gray-300);
    border-top: 0.125rem solid var(--color-primary);
    border-radius: 50%;
    animation: spin 1s linear infinite;
}

.rotating {
    animation: spin 1s linear infinite;
}

@keyframes spin {
    0% {
        transform: rotate(0deg);
    }
    100% {
        transform: rotate(360deg);
    }
}

/* Responsive adjustments */
@media (max-width: 768px) {
    .widget-title {
        font-size: 0.8125rem;
    }

    .widget-subtitle {
        display: none;
    }

    .widget-body {
        padding: 0.75rem;
    }

    .widget-header {
        padding: 0.625rem 0.75rem;
    }
}

</style>