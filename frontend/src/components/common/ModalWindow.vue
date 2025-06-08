<template>
    <teleport to="body">
        <transition name="modal-fade">
            <div v-if="isOpen" class="modal-overlay" @click.self="closeModal">
                <div class="modal-window">
                    <header class="modal-header">
                        <slot name="header">
                            <h2>{{ title }}</h2>
                        </slot>
                        <button class="close-button" @click="closeModal" aria-label="Close modal">&times;</button>
                    </header>
                    <main class="modal-body">
                        <slot></slot> </main>
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
@use "@/styles/components/modal" as modal;

.modal-overlay {
    display: flex;
    justify-content: center;
    align-items: center;
    position: fixed;
    top: 0;
    left: 0;
    width: 100vw;
    height: 100vh;
    background-color: modal.$modal-overlay-bg;
    z-index: map.get(vars.$z-layers, "modal-backdrop");
}

.modal-window {
    display: flex;
    flex-direction: column;
    background-color: vars.$color-white;
    border-radius: vars.$border-radius-standard;
    box-shadow: vars.$shadow-lg;
    width: 90%;
    max-width: 600px;
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