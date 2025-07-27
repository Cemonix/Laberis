<template>
    <button :class="buttonClass" v-bind="$attrs">
        <slot></slot>
    </button>
</template>

<script setup lang="ts">
import {computed} from 'vue';

const props = withDefaults(defineProps<{
    variant?: 'primary' | 'secondary' | 'danger' | 'success';
}>(), {
    variant: 'primary',
});

const buttonClass = computed(() => [
    'btn',
    `btn-${props.variant}`
]);
</script>

<style lang="scss" scoped>
.btn {
    display: inline-block;
    padding: 0.5rem 1rem;
    border: 1px solid transparent;
    border-radius: 2px;
    cursor: pointer;
    font-weight: bold;
    text-align: center;
    text-decoration: none;
    transition: background-color 0.2s ease-in-out;
}

.btn-primary {
    background-color: var(--color-primary);
    color: var(--color-white);

    &:hover {
        background-color: var(--color-primary-hover);
    }
}

.btn-secondary {
    background-color: var(--color-secondary);
    color: var(--color-white);

    &:hover {
        background-color: var(--color-secondary-hover);
    }
}

.btn-danger {
    background-color: var(--color-red-600);
    color: var(--color-white);

    &:hover {
        background-color: var(--color-red-700);
    }
}

.btn-success {
    background-color: var(--color-green-600);
    color: var(--color-white);

    &:hover {
        background-color: var(--color-green-700);
    }
}

.btn:disabled {
    background-color: var(--color-gray-50);
    color: var(--color-gray-600);
    cursor: not-allowed;
}

.btn:disabled:hover {
    background-color: var(--color-gray-300);
}

</style>