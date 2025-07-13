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
import {onUnmounted, ref, watch} from 'vue';
import ModalWindow from '@/components/common/modal/ModalWindow.vue';
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

<style scoped>
.alert-message {
    font-size: 1rem;
    color: var(--color-gray-800);
    line-height: 1.5;
}
</style>