<template>
    <div class="card scheme-card">
        <header class="card-header">
            <h3 class="scheme-name">{{ scheme.name }}</h3>
            <div class="card-actions">
                <button class="btn btn-primary">Add Label</button>
                <button class="btn btn-secondary">Edit Scheme</button>
            </div>
        </header>
        <div class="card-body">
            <p v-if="scheme.description" class="scheme-description">{{ scheme.description }}</p>
            <div class="labels-container">
                <LabelChip
                    v-for="label in scheme.labels"
                    :key="label.labelId"
                    :label="label"
                />
                <p v-if="!scheme.labels || scheme.labels.length === 0" class="no-labels">
                    This scheme has no labels.
                </p>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import type { LabelScheme } from '@/types/label/labelScheme';
import LabelChip from './LabelChip.vue';

defineProps<{
    scheme: LabelScheme;
}>();
</script>

<style lang="scss" scoped>
@use "sass:color";
@use "@/styles/variables.scss" as vars;

.scheme-name {
    font-size: vars.$font_size_large;
    margin: 0;
}

.card-actions {
    display: flex;
    gap: vars.$padding-small;
}

.scheme-description {
    font-style: italic;
    color: color.adjust(vars.$theme-text, $lightness: 40%);
    margin-bottom: vars.$padding-medium;
}

.labels-container {
    display: flex;
    flex-wrap: wrap;
    gap: vars.$padding-small;
}

.no-labels {
    color: color.adjust(vars.$theme-text, $lightness: 50%);
    font-size: vars.$font_size_small;
}
</style>