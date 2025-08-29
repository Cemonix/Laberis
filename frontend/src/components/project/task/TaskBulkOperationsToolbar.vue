<template>
    <Transition name="slide-down">
        <div v-show="selectionCount > 0" class="bulk-operations-toolbar">
            <div class="toolbar-content">
                <!-- Selection info -->
                <div class="selection-info">
                    <font-awesome-icon :icon="faCheckSquare" class="selection-icon" />
                    <span class="selection-text">
                        {{ selectionCount }} task{{ selectionCount === 1 ? '' : 's' }} selected
                    </span>
                </div>

                <!-- Bulk actions -->
                <div class="bulk-actions">
                    <!-- Clear selection -->
                    <Button 
                        variant="secondary" 
                        size="small"
                        @click="handleClearSelection"
                        :disabled="isOperationInProgress"
                        title="Clear selection"
                    >
                        Clear All
                    </Button>

                    <!-- Priority change dropdown -->
                    <div class="dropdown-wrapper">
                        <Button 
                            variant="secondary" 
                            size="small"
                            @click="togglePriorityDropdown"
                            :disabled="isOperationInProgress"
                            class="dropdown-trigger"
                        >
                            Priority
                            <font-awesome-icon :icon="faChevronDown" class="dropdown-arrow" />
                        </Button>
                        
                        <div v-if="showPriorityDropdown" class="dropdown-menu priority-dropdown">
                            <button 
                                v-for="option in priorityOptions" 
                                :key="option.value"
                                class="dropdown-item"
                                @click="handleBulkPriorityChange(option.value)"
                                :disabled="isOperationInProgress"
                            >
                                <font-awesome-icon :icon="option.icon" :class="option.class" />
                                <span>Set to {{ option.label }}</span>
                            </button>
                        </div>
                    </div>

                    <!-- Assignment dropdown -->
                    <div class="dropdown-wrapper">
                        <Button 
                            variant="secondary" 
                            size="small"
                            @click="toggleAssignDropdown"
                            :disabled="isOperationInProgress"
                            class="dropdown-trigger"
                        >
                            Assign
                            <font-awesome-icon :icon="faChevronDown" class="dropdown-arrow" />
                        </Button>
                        
                        <div v-if="showAssignDropdown" class="dropdown-menu assign-dropdown">
                            <button 
                                class="dropdown-item"
                                @click="handleBulkAssignment(null)"
                                :disabled="isOperationInProgress"
                            >
                                <font-awesome-icon :icon="faUserSlash" />
                                <span>Unassign all</span>
                            </button>
                            
                            <div class="dropdown-separator"></div>
                            
                            <button 
                                v-for="member in teamMembers" 
                                :key="member.email"
                                class="dropdown-item"
                                @click="handleBulkAssignment(member.email)"
                                :disabled="isOperationInProgress"
                            >
                                <font-awesome-icon :icon="faUser" />
                                <span>{{ member.email }}</span>
                            </button>
                        </div>
                    </div>

                </div>

                <!-- Operation progress -->
                <div v-if="isOperationInProgress" class="operation-progress">
                    <div class="progress-spinner">
                        <font-awesome-icon :icon="faSpinner" spin />
                    </div>
                    <span class="progress-text">{{ operationProgressText }}</span>
                </div>
            </div>
        </div>
    </Transition>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue';
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';
import { 
    faCheckSquare, 
    faChevronDown, 
    faExclamationTriangle, 
    faArrowUp, 
    faMinus,
    faUser,
    faUserSlash,
    faSpinner
} from '@fortawesome/free-solid-svg-icons';
import Button from '@/components/common/Button.vue';
import type { ProjectMember } from '@/services/project/projectMember.types';

interface Props {
    /** Number of selected tasks */
    selectionCount: number;
    /** Available team members for assignment */
    teamMembers: ProjectMember[];
    /** Whether a bulk operation is currently in progress */
    isOperationInProgress?: boolean;
    /** Progress text for current operation */
    operationProgressText?: string;
}

interface Emits {
    (e: 'clear-selection'): void;
    (e: 'bulk-priority-change', priority: number): void;
    (e: 'bulk-assignment', email: string | null): void;
}

withDefaults(defineProps<Props>(), {
    isOperationInProgress: false,
    operationProgressText: 'Processing...'
});

const emit = defineEmits<Emits>();

// Dropdown states
const showPriorityDropdown = ref(false);
const showAssignDropdown = ref(false);

// Priority options configuration
const priorityOptions = [
    {
        value: 1,
        label: 'Low',
        icon: faMinus,
        class: 'priority-low'
    },
    {
        value: 2,
        label: 'Medium',
        icon: faArrowUp,
        class: 'priority-medium'
    },
    {
        value: 3,
        label: 'High',
        icon: faExclamationTriangle,
        class: 'priority-high'
    }
];

// Event handlers
const handleClearSelection = () => {
    emit('clear-selection');
};

const handleBulkPriorityChange = (priority: number) => {
    showPriorityDropdown.value = false;
    emit('bulk-priority-change', priority);
};

const handleBulkAssignment = (email: string | null) => {
    showAssignDropdown.value = false;
    emit('bulk-assignment', email);
};


// Dropdown toggle handlers
const togglePriorityDropdown = () => {
    showPriorityDropdown.value = !showPriorityDropdown.value;
    showAssignDropdown.value = false;
};

const toggleAssignDropdown = () => {
    showAssignDropdown.value = !showAssignDropdown.value;
    showPriorityDropdown.value = false;
};

// Close dropdowns when clicking outside
const handleClickOutside = (event: MouseEvent) => {
    const target = event.target as Element;
    if (!target.closest('.dropdown-wrapper')) {
        showPriorityDropdown.value = false;
        showAssignDropdown.value = false;
    }
};

onMounted(() => {
    document.addEventListener('click', handleClickOutside);
});

onUnmounted(() => {
    document.removeEventListener('click', handleClickOutside);
});
</script>

<style lang="scss" scoped>
.bulk-operations-toolbar {
    position: sticky;
    top: 0;
    z-index: 10;
    background: var(--color-primary-50);
    border: 1px solid var(--color-primary-200);
    border-radius: 6px;
    margin-bottom: 1rem;
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.toolbar-content {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 0.75rem 1rem;
    gap: 1rem;
}

.selection-info {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    color: var(--color-primary-700);
    font-weight: 500;
    
    .selection-icon {
        font-size: 1rem;
    }
    
    .selection-text {
        font-size: 0.875rem;
    }
}

.bulk-actions {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    flex-wrap: wrap;
}

.dropdown-wrapper {
    position: relative;
}

.dropdown-trigger {
    display: flex;
    align-items: center;
    gap: 0.25rem;
    
    .dropdown-arrow {
        font-size: 0.75rem;
        transition: transform 0.2s ease;
    }
}

.dropdown-menu {
    position: absolute;
    top: 100%;
    left: 0;
    min-width: 200px;
    background: var(--color-white);
    border: 1px solid var(--color-gray-300);
    border-radius: 6px;
    box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
    z-index: 1000;
    margin-top: 0.25rem;
}

.dropdown-item {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    width: 100%;
    padding: 0.5rem 0.75rem;
    border: none;
    background: none;
    text-align: left;
    font-size: 0.875rem;
    cursor: pointer;
    transition: background-color 0.2s ease;
    
    &:first-child {
        border-top-left-radius: 5px;
        border-top-right-radius: 5px;
    }
    
    &:last-child {
        border-bottom-left-radius: 5px;
        border-bottom-right-radius: 5px;
    }
    
    &:hover:not(:disabled) {
        background: var(--color-gray-50);
    }
    
    &:disabled {
        cursor: not-allowed;
        opacity: 0.5;
    }
    
    // Priority-specific colors
    &.priority-low {
        color: var(--color-gray-600);
    }
    
    &.priority-medium {
        color: var(--color-warning);
    }
    
    &.priority-high {
        color: var(--color-error);
    }
}

.dropdown-separator {
    height: 1px;
    background: var(--color-gray-200);
    margin: 0.25rem 0;
}

.operation-progress {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    color: var(--color-primary-600);
    font-size: 0.875rem;
    
    .progress-spinner {
        display: flex;
        align-items: center;
    }
}

// Transitions
.slide-down-enter-active,
.slide-down-leave-active {
    transition: all 0.3s ease;
    transform-origin: top;
}

.slide-down-enter-from {
    opacity: 0;
    transform: translateY(-10px) scaleY(0.8);
}

.slide-down-leave-to {
    opacity: 0;
    transform: translateY(-10px) scaleY(0.8);
}

// Responsive design
@media (max-width: 768px) {
    .toolbar-content {
        flex-direction: column;
        align-items: stretch;
        gap: 0.75rem;
    }
    
    .bulk-actions {
        justify-content: center;
        flex-wrap: wrap;
    }
    
    .selection-info {
        justify-content: center;
    }
}

@media (max-width: 480px) {
    .bulk-actions {
        display: grid;
        grid-template-columns: repeat(2, 1fr);
        gap: 0.5rem;
        
        .dropdown-wrapper,
        button {
            width: 100%;
        }
    }
}
</style>