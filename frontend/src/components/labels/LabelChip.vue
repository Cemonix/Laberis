<template>
    <div class="label-chip" :style="{ backgroundColor: chipColor, color: textColor }">
        {{ label.name }}
    </div>
</template>

<script setup lang="ts">
import {computed} from 'vue';
import type {Label} from '@/types/label/label';

const props = defineProps<{
    label: Label;
}>();

const chipColor = computed(() => {
    // TODO: This is a simple way to adjust color, but for true WCAG contrast, a more robust library might be needed.
    return props.label.color + '33';
});

const textColor = computed(() => props.label.color);
</script>

<style scoped>
.label-chip {
    display: inline-block;
    padding: 0.25rem 0.5rem;
    border-radius: 1em;
    font-size: 0.875rem;
    font-weight: var(--font_weight_large);
    white-space: nowrap;
}
</style>