<template>
    <div :class="['base-card', $attrs.class]" v-bind="$attrs">
        <header v-if="$slots.header" class="base-card-header">
            <slot name="header"></slot>
        </header>
        <div class="base-card-body">
            <slot></slot>
		</div>
        <footer v-if="$slots.footer" class="base-card-footer">
            <slot name="footer"></slot>
        </footer>
    </div>
</template>

<script setup lang="ts"></script>

<style lang="scss" scoped>
@use "@/styles/variables" as vars;

.base-card {
    display: flex;
    flex-direction: column;
    flex-grow: 1;
    background-color: vars.$theme-surface;
    border: vars.$border-width solid vars.$theme-border;
    border-radius: vars.$border-radius-standard;
    box-shadow: vars.$shadow-sm;
    text-decoration: none;
    color: inherit;
}

.base-card-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: vars.$padding-medium;
    border-bottom: vars.$border-width solid vars.$theme-border;
    color: vars.$color-gray-900;
}

.base-card-body {
    padding: vars.$padding-medium;
    flex-grow: 1;
    color: vars.$color-gray-800;
}

.base-card-footer {
	display: flex;
    justify-content: space-between;
    align-items: center;
    gap: vars.$gap-medium;
    font-size: vars.$font_size_small;
    text-transform: capitalize;
    padding: vars.$padding-medium;
    border-top: vars.$border-width solid vars.$theme-border;
    color: vars.$color-gray-600;
}
</style>