<template>
    <button :class="buttonClass" v-bind="$attrs">
        <slot></slot>
    </button>
</template>

<script setup lang="ts">
import { computed } from 'vue';

const props = withDefaults(defineProps<{
    variant?: 'primary' | 'secondary';
}>(), {
    variant: 'primary',
});

const buttonClass = computed(() => [
    'btn',
    `btn-${props.variant}`
]);
</script>

<style lang="scss" scoped>
@use "@/styles/variables" as vars;

.btn {
    display: inline-block;
    padding: vars.$padding-small vars.$padding-medium;
    border: 1px solid transparent;
    border-radius: vars.$border-radius-input;
    cursor: pointer;
    font-weight: bold;
    text-align: center;
    text-decoration: none;
    transition: background-color 0.2s ease-in-out;
}

.btn-primary {
    background-color: vars.$color-primary;
    color: vars.$color-white;

    &:hover {
        background-color: vars.$color-primary-hover;
    }
}

.btn-secondary {
    background-color: vars.$color-secondary;
    color: vars.$color-white;

    &:hover {
        background-color: vars.$color-secondary-hover;
    }
}

.btn:disabled {
    background-color: vars.$color-gray-300;
    color: vars.$color-gray-600;
    cursor: not-allowed;
}

.btn:disabled:hover {
    background-color: vars.$color-gray-300;
}

</style>