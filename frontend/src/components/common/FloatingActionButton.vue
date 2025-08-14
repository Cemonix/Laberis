<template>
    <Teleport to="#fab-container" :disabled="!shouldShow">
        <Transition name="fab" appear>
            <button
                v-if="shouldShow"
                class="fab"
                @click="handleClick"
                :disabled="disabled"
                :aria-label="ariaLabel"
                :title="title"
            >
                <font-awesome-icon v-if="icon" :icon="icon" />
                <slot v-else>{{ text || '+' }}</slot>
            </button>
        </Transition>
    </Teleport>
</template>

<script setup lang="ts">
import { computed, ref, onMounted } from 'vue';
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';
import type { IconDefinition } from '@fortawesome/fontawesome-svg-core';
import { usePermissions } from '@/composables/usePermissions';

interface Props {
    onClick?: () => void;
    icon?: IconDefinition;
    text?: string;
    disabled?: boolean;
    /** Accessibility label */
    ariaLabel?: string;
    /** Tooltip text */
    title?: string;
    /** Required permission */
    permission?: string;
    /** Multiple required permissions */
    permissions?: string[];
    /** Permission check mode: 'all' or 'any' */
    permissionMode?: 'all' | 'any';
    /** Check global permissions instead of project permissions */
    globalPermissions?: boolean;
    animationDelay?: number;
    /** Force show button regardless of permissions (for testing) */
    forceShow?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
    text: '+',
    disabled: false,
    ariaLabel: 'Add new item',
    permissions: () => [],
    permissionMode: 'all',
    globalPermissions: false,
    animationDelay: 50,
    forceShow: false
});

const emit = defineEmits<{
    click: [];
}>();

const {
    hasAnyGlobalPermission,
    hasAllGlobalPermissions,
    hasAnyProjectPermission,
    hasAllProjectPermissions
} = usePermissions();

const isVisible = ref(false);

// Check if user has required permissions
const hasPermission = computed(() => {
    if (props.forceShow) return true;
    
    // Build the list of permissions to check
    const permissionsToCheck: string[] = [];
    if (props.permission) {
        permissionsToCheck.push(props.permission);
    }
    if (props.permissions && props.permissions.length > 0) {
        permissionsToCheck.push(...props.permissions);
    }
    
    // If no permissions specified, show the button by default
    if (permissionsToCheck.length === 0) {
        return true;
    }
    
    if (props.globalPermissions) {
        // Check global permissions
        return props.permissionMode === 'all' 
            ? hasAllGlobalPermissions(permissionsToCheck)
            : hasAnyGlobalPermission(permissionsToCheck);
    } else {
        // Check project permissions
        return props.permissionMode === 'all'
            ? hasAllProjectPermissions(permissionsToCheck)
            : hasAnyProjectPermission(permissionsToCheck);
    }
});

// Only show if user has permissions and visibility is enabled
const shouldShow = computed(() => hasPermission.value && isVisible.value);

const handleClick = () => {
    if (!props.disabled && props.onClick) {
        props.onClick();
    }
    emit('click');
};

// Delayed visibility to prevent layout shifts
onMounted(() => {
    setTimeout(() => {
        isVisible.value = true;
    }, props.animationDelay);
});
</script>

<style scoped>
.fab {
    display: flex;
    justify-content: center;
    align-items: center;
    position: absolute;
    bottom: 3.5rem;
    right: 1.5rem;
    width: 60px;
    height: 60px;
    border-radius: 50%;
    background-color: var(--color-primary);
    color: var(--color-white);
    border: none;
    font-size: 2.5rem;
    line-height: 1;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15), 0 2px 4px rgba(0, 0, 0, 0.12);
    cursor: pointer;
    padding-bottom: 4px;
    transition: all 0.2s ease-in-out;
    z-index: 50;
    
    &:hover:not(:disabled) {
        background-color: var(--color-primary-hover);
        transform: scale(1.1);
        box-shadow: 0 6px 16px rgba(0, 0, 0, 0.2), 0 3px 6px rgba(0, 0, 0, 0.16);
    }
    
    &:active:not(:disabled) {
        transform: scale(1.05);
        transition: transform 0.1s ease-in-out;
    }
    
    &:disabled {
        background-color: var(--color-gray-400);
        cursor: not-allowed;
        opacity: 0.6;
    }
    
    &:focus-visible {
        outline: 2px solid var(--color-primary);
        outline-offset: 2px;
    }
}

/* Animation transitions */
.fab-enter-active, .fab-leave-active {
    transition: all 0.3s cubic-bezier(0.68, -0.55, 0.265, 1.55);
}

.fab-enter-from {
    transform: scale(0) rotate(-180deg);
    opacity: 0;
}

.fab-leave-to {
    transform: scale(0) rotate(180deg);
    opacity: 0;
}

/* Responsive adjustments */
@media (max-width: 768px) {
    .fab {
        bottom: 1.5rem;
        right: 1.5rem;
        width: 56px;
        height: 56px;
        font-size: 2.2rem;
    }
}

/* High contrast mode support */
@media (prefers-contrast: high) {
    .fab {
        border: 2px solid var(--color-primary-contrast, var(--color-white));
    }
}

/* Reduced motion support */
@media (prefers-reduced-motion: reduce) {
    .fab {
        transition: none;
    }
    
    .fab-enter-active, .fab-leave-active {
        transition: opacity 0.2s ease;
    }
    
    .fab-enter-from, .fab-leave-to {
        transform: none;
        rotate: none;
    }
    
    .fab:hover:not(:disabled) {
        transform: none;
    }
}
</style>