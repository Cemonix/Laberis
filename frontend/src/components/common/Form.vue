<template>
    <form
        :class="['base-form', $attrs.class]"
        v-bind="$attrs"
        @submit.prevent="$emit('submit', $event)"
    >
        <slot />
    </form>
</template>

<script setup lang="ts">
defineEmits(["submit"]);
</script>

<style lang="scss" scoped>
.base-form {
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

:deep(.form-group) {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;

    label {
        font-weight: bold;
        color: var(--color-gray-800);
    }

    input[type="text"],
    input[type="email"],
    input[type="password"],
    textarea,
    select {
        width: 100%;
        padding: 0.5rem;
        border: 1px solid var(--color-gray-400);
        border-radius: 2px;
        font-size: 1rem;
        font-family: var(--font-family-body), sans-serif;
    }
}

:deep(.form-actions) {
    display: flex;
    justify-content: flex-end;
    align-items: center;
    gap: 0.5rem;
    margin-top: 1rem;
    padding-top: 1rem;
    border-top: 1px solid var(--color-gray-400);
}
</style>
