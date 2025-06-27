<template>
    <ModalWindow 
        :is-open="isOpen" 
        :title="title"
        @close="handleCancel" 
        class="confirm-modal"
    >
        <p class="confirm-message">{{ message }}</p>
        <template #footer>
            <Button @click="handleConfirm" v-bind="$attrs">
                OK
            </Button>
            <Button @click="handleCancel" v-bind="$attrs" class="cancel-button">
                Cancel
            </Button>
        </template>
    </ModalWindow>
</template>

<script setup lang="ts">
import { watch, ref, onUnmounted } from 'vue';
import ModalWindow from '@/components/common/modal/ModalWindow.vue';
import Button from '@/components/common/Button.vue';

const props = defineProps<{
    isOpen: boolean;
    title: string;
    message: string;
}>();

const canCloseWithEnter = ref(false);
const canCloseWithEscape = ref(false);

const emit = defineEmits<{
    (e: 'confirm'): void;
    (e: 'cancel'): void;
}>();

const handleConfirm = () => {
    emit('confirm');
};

const handleCancel = () => {
    emit('cancel');
};

const onEnter = (e: KeyboardEvent) => {
    if (props.isOpen && e.key === 'Enter' && canCloseWithEnter.value) {
        e.preventDefault();
        handleConfirm();
    }
};

const onEscape = (e: KeyboardEvent) => {
    if (props.isOpen && e.key === 'Escape' && canCloseWithEscape.value) {
        e.preventDefault();
        handleCancel();
    }
};

watch(() => props.isOpen, (isOpen) => {
    if (isOpen) {
        document.addEventListener('keydown', onEnter);
        document.addEventListener('keydown', onEscape);
        
        // Allow a brief delay to ensure the event listener doesn't catch other events from parent components
        setTimeout(() => {
            canCloseWithEnter.value = true;
            canCloseWithEscape.value = true;
        }, 50);

    } else {
        canCloseWithEnter.value = false;
        canCloseWithEscape.value = false;
        document.removeEventListener('keydown', onEnter);
        document.removeEventListener('keydown', onEscape);
    }
}, { immediate: true });

onUnmounted(() => {
    document.removeEventListener('keydown', onEnter);
    document.removeEventListener('keydown', onEscape);
});
</script>

<style lang="scss" scoped>
@use "@/styles/variables" as vars;

.confirm-message {
    font-size: vars.$font-size-medium;
    color: vars.$theme-text;
    line-height: vars.$line-height-large;
}

.cancel-button {
    background-color: vars.$color-error-dark;
    color: vars.$color-white;
    margin-left: vars.$margin-small;

    &:hover {
        background-color: vars.$color-error-darker;
    }
}
</style>