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
@use "sass:color";
@use "@/styles/variables" as vars;

.base-form {
    display: flex;
    flex-direction: column;
    gap: vars.$gap-medium;
}

:deep(.form-group) {
    display: flex;
    flex-direction: column;
    gap: vars.$gap-tiny;

    label {
        font-weight: bold;
        color: vars.$theme-text-light;
    }

    input[type="text"],
    input[type="email"],
    input[type="password"],
    textarea,
    select {
        width: 100%;
        padding: vars.$padding-small;
        border: vars.$border-width solid vars.$color-gray-400;
        border-radius: vars.$border-radius-sm;
        font-size: vars.$font-size-medium;
        font-family: vars.$font-family-body;
    }
}

:deep(.form-actions) {
    display: flex;
    justify-content: flex-end;
    align-items: center;
    gap: vars.$gap-small;
    margin-top: vars.$margin-medium;
    padding-top: vars.$padding-medium;
    border-top: vars.$border-width solid vars.$color-gray-400;
}
</style>
