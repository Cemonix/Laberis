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
import { watch } from 'vue';
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
@use "sass:color";
@use "sass:map";
@use "@/styles/variables" as vars;

.modal-overlay {
    display: flex;
    justify-content: center;
    align-items: center;
    position: fixed;
    top: 0;
    left: 0;
    width: 100vw;
    height: 100vh;
    background-color: rgba(vars.$color-black, 0.6);
    z-index: vars.$z-layer-base-modal;

    &.alert-modal {
        z-index: vars.$z-layer-alert-modal;
    }
}

.modal-window {
    display: flex;
    flex-direction: column;
    background-color: vars.$color-white;
    border-radius: vars.$border-radius;
    box-shadow: vars.$shadow-lg;
    width: 90%;
    max-width: 600px;
    position: relative;
}

.modal-header {
    padding: vars.$padding-medium;
    border-bottom: vars.$border-width solid vars.$theme-border;
    display: flex;
    justify-content: space-between;
    align-items: center;

    h2 {
        font-size: vars.$font_size_large;
        color: vars.$theme-text;
    }
}

.close-button {
    background: none;
    border: none;
    font-size: vars.$font_size_xxlarge;
    font-weight: 300;
    line-height: 1;
    cursor: pointer;
    color: vars.$theme-text-light;
    padding: 0;
}

.modal-body {
    padding: vars.$padding-large;
    overflow-y: auto;
}

.modal-footer {
    padding: vars.$padding-medium;
    border-top: vars.$border-width solid vars.$theme-border;
    display: flex;
    justify-content: flex-end;
    gap: vars.$gap-small;
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