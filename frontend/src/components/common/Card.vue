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

<style scoped>
.base-card {
    display: flex;
    flex-direction: column;
    flex-grow: 1;
    background-color: var(--color-white);
    border: 1px solid var(--color-gray-400);
    border-radius: 4px;
    box-shadow: 0 1px 3px rgba(var(--color-black), 0.05);
    text-decoration: none;
    color: inherit;
}

.base-card-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 1rem;
    border-bottom: 1px solid var(--color-gray-400);
    color: var(--color-gray-900);
}

.base-card-body {
    padding: 1rem;
    flex-grow: 1;
    color: var(--color-gray-800);
}

.base-card-footer {
	display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 1rem;
    font-size: 0.875rem;
    text-transform: capitalize;
    padding: 1rem;
    border-top: 1px solid var(--color-gray-400);
    color: var(--color-gray-600);
}
</style>