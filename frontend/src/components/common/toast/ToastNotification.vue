<template>
    <Transition 
        name="toast" 
        appear
        @enter="onEnter"
        @leave="onLeave"
    >
        <div 
            v-if="visible"
            :class="toastClasses" 
            class="toast-notification"
            role="alert"
            :aria-labelledby="`toast-title-${toast.id}`"
            :aria-describedby="`toast-message-${toast.id}`"
        >
            <div class="toast-icon">
                <font-awesome-icon :icon="iconName" />
            </div>
            
            <div class="toast-content">
                <h4 :id="`toast-title-${toast.id}`" class="toast-title">
                    {{ toast.title }}
                </h4>
                <p :id="`toast-message-${toast.id}`" class="toast-message">
                    {{ toast.message }}
                </p>
            </div>
            
            <button 
                class="toast-close"
                @click="handleClose"
                type="button"
                aria-label="Close notification"
            >
                <font-awesome-icon :icon="faTimes" />
            </button>
            
            <!-- Progress bar for auto-dismiss -->
            <div 
                v-if="!toast.persistent && toast.duration && toast.duration > 0"
                class="toast-progress"
                :style="{ animationDuration: `${toast.duration}ms` }"
            ></div>
        </div>
    </Transition>
</template>

<script setup lang="ts">
import {computed, onMounted, ref} from 'vue';
import {FontAwesomeIcon} from '@fortawesome/vue-fontawesome';
import {
    faCheckCircle,
    faExclamationCircle,
    faExclamationTriangle,
    faInfoCircle,
    faTimes
} from '@fortawesome/free-solid-svg-icons';
import type {Toast} from '@/composables/useToast';

const props = defineProps<{
    toast: Toast;
}>();

const emit = defineEmits<{
    (e: 'remove', id: string): void;
}>();

const visible = ref(false);

const iconName = computed(() => {
    switch (props.toast.type) {
        case 'success':
            return faCheckCircle;
        case 'error':
            return faExclamationCircle;
        case 'warning':
            return faExclamationTriangle;
        case 'info':
        default:
            return faInfoCircle;
    }
});

const toastClasses = computed(() => [
    'toast-notification',
    `toast-notification--${props.toast.type}`
]);

const handleClose = () => {
    visible.value = false;
};

const onEnter = () => {
    // Animation handled by CSS
};

const onLeave = () => {
    emit('remove', props.toast.id);
};

onMounted(() => {
    visible.value = true;
});
</script>

<style lang="scss" scoped>
.toast-notification {
    display: flex;
    align-items: flex-start;
    gap: 1rem;
    padding: 1rem;
    background: var(--color-white);
    border-radius: 8px;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1), 0 2px 4px rgba(0, 0, 0, 0.06);
    border-left: 4px solid;
    min-width: 320px;
    max-width: 400px;
    position: relative;
    overflow: hidden;

    &--success {
        border-left-color: var(--color-success);
        
        .toast-icon {
            color: var(--color-success);
        }
    }

    &--error {
        border-left-color: var(--color-error);
        
        .toast-icon {
            color: var(--color-error);
        }
    }

    &--warning {
        border-left-color: var(--color-warning);
        
        .toast-icon {
            color: var(--color-warning);
        }
    }

    &--info {
        border-left-color: var(--color-info);
        
        .toast-icon {
            color: var(--color-info);
        }
    }
}

.toast-icon {
    flex-shrink: 0;
    margin-top: 0.125rem;
    font-size: 1.25rem;
}

.toast-content {
    flex: 1;
    min-width: 0;
}

.toast-title {
    font-size: 1rem;
    font-weight: 600;
    color: var(--color-text-primary);
    margin: 0 0 0.25rem 0;
    line-height: 1.4;
}

.toast-message {
    font-size: 0.875rem;
    color: var(--color-text-secondary);
    margin: 0;
    line-height: 1.5;
    word-wrap: break-word;
}

.toast-close {
    flex-shrink: 0;
    background: none;
    border: none;
    color: var(--color-text-muted);
    cursor: pointer;
    padding: 0.25rem;
    margin: calc(-1 * 0.25rem) calc(-1 * 0.25rem) calc(-1 * 0.25rem) 0.5rem;
    border-radius: 2px;
    transition: color 0.2s ease, background-color 0.2s ease;
    font-size: 1rem;

    &:hover {
        color: var(--color-text-secondary);
        background-color: var(--color-gray-100);
    }

    &:focus {
        outline: 2px solid var(--color-primary);
        outline-offset: 2px;
    }
}

.toast-progress {
    position: absolute;
    bottom: 0;
    left: 0;
    height: 3px;
    background: linear-gradient(
        90deg,
            var(--color-primary) 0%,
            var(--color-primary-hover) 100%
    );
    animation: toast-progress linear forwards;
    transform-origin: left;
}

@keyframes toast-progress {
    from {
        transform: scaleX(1);
    }
    to {
        transform: scaleX(0);
    }
}

/* Toast transitions */
.toast-enter-active {
    transition: all 0.3s cubic-bezier(0.34, 1.56, 0.64, 1);
}

.toast-leave-active {
    transition: all 0.3s ease-in;
}

.toast-enter-from {
    opacity: 0;
    transform: translateX(100%) scale(0.9);
}

.toast-leave-to {
    opacity: 0;
    transform: translateX(100%) scale(0.9);
}
</style>
