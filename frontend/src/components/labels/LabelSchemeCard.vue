<template>
    <Card class="scheme-card">
        <template #header>
            <h3 class="scheme-name">{{ scheme.name }}</h3>
            <div class="card-actions">
                <Button variant="primary">Add Label</Button>
                <Button variant="secondary">Edit Scheme</Button>
            </div>
        </template>

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
    </Card>
</template>

<script setup lang="ts">
import type { LabelScheme } from '@/types/label/labelScheme';
import LabelChip from './LabelChip.vue';
import Button from '@/components/common/Button.vue';
import Card from '@/components/common/Card.vue';

defineProps<{
    scheme: LabelScheme;
}>();
</script>

<style lang="scss" scoped>
@use "sass:color";
@use "@/styles/variables" as vars;

.scheme-name {
    font-size: vars.$font_size_large;
}

.card-actions {
    display: flex;
    gap: vars.$gap-small;
}

.scheme-description {
    font-style: italic;
    color: vars.$theme-text-light;
    margin-bottom: vars.$margin-medium;
}

.labels-container {
    display: flex;
    flex-wrap: wrap;
    gap: vars.$gap-small;
}

.no-labels {
    color: vars.$theme-text-light;
    font-size: vars.$font_size_small;
}
</style>