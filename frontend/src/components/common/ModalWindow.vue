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
@use "@/styles/mixins" as mixins;

.modal-overlay {
    position: fixed;
    top: 0;
    left: 0;
    width: 100vw;
    height: 100vh;
    background-color: vars.$modal-overlay-bg;
    z-index: map.get(vars.$z-layers, "modal-backdrop");
    @include mixins.flex-center;
}

.modal-window {
    background-color: vars.$color-white;
    border-radius: vars.$border-radius-standard;
    box-shadow: vars.$shadow-lg;
    width: 90%;
    max-width: 600px;
    @include mixins.flex-column;
}

.modal-header {
    padding: vars.$padding-medium;
    border-bottom: vars.$border-width solid vars.$theme-border;
    @include mixins.flexbox(
        $justify-content: space-between,
        $align-items: center
    );

    h2 {
        margin: 0;
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
    color: color.adjust(vars.$theme-text, $lightness: 40%);
    padding: 0;
}

.modal-body {
    padding: vars.$padding-large;
    overflow-y: auto;
}

.modal-footer {
    padding: vars.$padding-medium;
    border-top: vars.$border-width solid vars.$theme-border;
    @include mixins.flexbox(
        $justify-content: flex-end,
        $gap: vars.$padding-small
    );
}

.modal-fade-enter-active,
.modal-fade-leave-active {
    transition: opacity vars.$transition-slow-ease;
}
.modal-fade-enter-from,
.modal-fade-leave-to {
    opacity: 0;
}
</style>