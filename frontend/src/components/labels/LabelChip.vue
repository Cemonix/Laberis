<template>
    <div class="label-chip" :style="{ backgroundColor: chipColor, color: textColor }">
        {{ label.name }}
    </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { Label } from '@/types/label/label';

const props = defineProps<{
    label: Label;
}>();

const chipColor = computed(() => {
    // TODO: This is a simple way to adjust color, but for true WCAG contrast, a more robust library might be needed.
    return props.label.color + '33';
});

const textColor = computed(() => props.label.color);
</script>

<style lang="scss" scoped>
@use "@/styles/variables" as vars;

.label-chip {
    display: inline-block;
    padding: 0.25em 0.75em;
    border-radius: 1em;
    font-size: vars.$font-size-small;
    font-weight: vars.$font_weight_heading;
    white-space: nowrap;
}
</style>