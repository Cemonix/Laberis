<template>
    <teleport to="body">
        <transition name="modal-fade">
            <div 
                v-if="isOpen" 
                class="modal-overlay" 
                :class="$attrs.class" 
                @click.self="closeModal"
            >
                <div class="modal-window">
                    <header class="modal-header">
                        <slot name="header">
                            <h2>{{ title }}</h2>
                        </slot>
                        <Button 
                            class="close-button" 
                            @click="closeModal" 
                            aria-label="Close modal"
                        >
                            &times;
                        </Button>
                    </header>
                    <div class="modal-body">
                        <slot></slot>
                    </div>
                    <footer v-if="!hideFooter" class="modal-footer">
                        <slot name="footer"></slot> 
                    </footer>
                </div>
            </div>
        </transition>
    </teleport>
</template>

<script setup lang="ts">
import {watch} from 'vue';
import Button from '@/components/common/Button.vue';

defineOptions({
    inheritAttrs: false
});

const props = defineProps<{
    isOpen: boolean;
    title?: string;
    hideFooter?: boolean;
}>();

const emit = defineEmits<{
    (e: 'close'): void;
}>();

const closeModal = () => {
    emit('close');
};

watch(() => props.isOpen, (newValue) => {
    const onEscape = (e: KeyboardEvent) => {
        if (e.key === 'Escape' && props.isOpen) {
            closeModal();
        }
    };

    if (newValue) {
        document.addEventListener('keydown', onEscape);
    } else {
        document.removeEventListener('keydown', onEscape);
    }
});
</script>

<style lang="scss" scoped>
.modal-overlay {
    display: flex;
    justify-content: center;
    align-items: center;
    position: fixed;
    top: 0;
    left: 0;
    width: 100vw;
    height: 100vh;
    background-color: rgba(0, 0, 0, 0.1);
    z-index: 100;

    &.alert-modal {
        z-index: 200;
    }
}

.modal-window {
    display: flex;
    flex-direction: column;
    background-color: var(--color-white);
    border-radius: 4px;
    box-shadow: 0 1px 3px rgba(0, 0, 0, 0.15);
    width: 90%;
    max-width: 600px;
    position: relative;
}

.modal-header {
    padding: 1rem;
    border-bottom: 1px solid var(--color-gray-400);
    display: flex;
    justify-content: space-between;
    align-items: center;

    h2 {
        font-size: 1.25rem;
        color: var(--color-gray-800);
    }
}

.close-button {
    background: none;
    border: none;
    font-size: 2rem;
    font-weight: 300;
    line-height: 1;
    cursor: pointer;
    color: var(--color-gray-600);
    padding: 0;

    &:hover {
        color: var(--color-error);
        background: none;
    }
}

.modal-body {
    padding: 1.5rem;
    overflow-y: auto;
}

.modal-footer {
    padding: 1rem;
    border-top: 1px solid var(--color-gray-400);
    display: flex;
    justify-content: flex-end;
    gap: 0.5rem;
}

.modal-fade-enter-active,
.modal-fade-leave-active {
    transition: opacity 0.3s ease;
}
.modal-fade-enter-from,
.modal-fade-leave-to {
    opacity: 0;
}
</style>