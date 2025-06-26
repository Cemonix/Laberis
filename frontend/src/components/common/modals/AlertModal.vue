<template>
    <ModalWindow 
        :is-open="isOpen" 
        :title="title"
        @close="handleConfirm" 
        class="alert-modal"
    >
        <p class="alert-message">{{ message }}</p>
        <template #footer>
            <Button @click="handleConfirm" v-bind="$attrs">
                OK
            </Button>
        </template>
    </ModalWindow>
</template>

<script setup lang="ts">
import { watch, ref, onUnmounted } from 'vue';
import ModalWindow from '@/components/common/modals/ModalWindow.vue';
import Button from '@/components/common/Button.vue';

const props = defineProps<{
    isOpen: boolean;
    title: string;
    message: string;
}>();

const canCloseWithEnter = ref(false);

const emit = defineEmits<{
    (e: 'confirm'): void;
}>();

const handleConfirm = () => {
    emit('confirm');
};

const onEnter = (e: KeyboardEvent) => {
    if (props.isOpen && e.key === 'Enter' && canCloseWithEnter.value) {
        e.preventDefault();
        handleConfirm();
    }
};

watch(() => props.isOpen, (isOpen) => {
    if (isOpen) {
        document.addEventListener('keydown', onEnter);
        
        // Allow a brief delay to ensure the event listener doesn't catch other events from parent components
        setTimeout(() => {
            canCloseWithEnter.value = true;
        }, 50);

    } else {
        canCloseWithEnter.value = false;
        document.removeEventListener('keydown', onEnter);
    }
}, { immediate: true });

onUnmounted(() => {
    document.removeEventListener('keydown', onEnter);
});
</script>

<style lang="scss" scoped>
@use "sass:map";
@use "@/styles/variables" as vars;

.alert-message {
    font-size: vars.$font-size-medium;
    color: vars.$theme-text;
    line-height: vars.$line-height-large;
}
</style>