<template>
    <Teleport to="body">
        <div 
            v-if="toasts.length > 0"
            class="toast-container"
            aria-live="polite"
            aria-label="Notifications"
        >
            <TransitionGroup
                name="toast-list"
                tag="div"
                class="toast-list"
            >
                <ToastNotification
                    v-for="toast in toasts"
                    :key="toast.id"
                    :toast="toast"
                    @remove="handleRemoveToast"
                />
            </TransitionGroup>
        </div>
    </Teleport>
</template>

<script setup lang="ts">
import {useToast} from '@/composables/useToast';
import ToastNotification from './ToastNotification.vue';

const { toasts, removeToast } = useToast();

const handleRemoveToast = (id: string) => {
    removeToast(id);
};
</script>

<style lang="scss" scoped>
.toast-container {
    position: fixed;
    top: 80px;
    right: 1.5rem;
    z-index: 9999;
    pointer-events: none;
    
    @media (max-width: 768px) {
        right: 1rem;
        left: 1rem;
        top: 70px;
    }
}

.toast-list {
    display: flex;
    flex-direction: column;
    align-items: flex-end;
    gap: 0.5rem;
    
    @media (max-width: 768px) {
        align-items: stretch;
    }
}

:deep(.toast-notification) {
    pointer-events: auto;
}

/* List transition animations */
.toast-list-enter-active {
    transition: all 0.3s cubic-bezier(0.34, 1.56, 0.64, 1);
}

.toast-list-leave-active {
    transition: all 0.3s ease-in;
}

.toast-list-enter-from {
    opacity: 0;
    transform: translateX(100%) scale(0.9);
}

.toast-list-leave-to {
    opacity: 0;
    transform: translateX(100%) scale(0.9);
}

.toast-list-move {
    transition: transform 0.3s ease;
}
</style>
