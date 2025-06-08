<template>
    <router-link :to="projectUrl" class="card project-card">
        <header class="card-header">
            <h3 class="project-name">{{ project.name }}</h3>
            <span class="project-status" :class="statusClass">{{ project.status }}</span>
        </header>
        <div class="card-body">
            <p class="project-description">{{ project.description || 'No description provided.' }}</p>
        </div>
        <footer class="card-footer">
            <span class="project-meta">{{ project.projectType.replace('_', ' ') }}</span>
            <span class="project-meta">Created: {{ formattedDate }}</span>
        </footer>
    </router-link>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { Project } from '@/types/project/project';

const props = defineProps<{
    project: Project;
}>();

const projectUrl = computed(() => `/projects/${props.project.projectId}`);
const statusClass = computed(() => `status-${props.project.status.toLowerCase()}`);

const formattedDate = computed(() => {
    return new Date(props.project.createdAt).toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'long',
        day: 'numeric',
    });
});
</script>

<style lang="scss" scoped>
@use "sass:color";
@use "@/styles/variables" as vars;

.project-card {
    transition: transform 0.2s ease-in-out, box-shadow 0.2s ease-in-out;
    height: 100%;

    &:hover {
        transform: translateY(-5px);
        box-shadow: vars.$shadow-md;
        border-color: color.adjust(vars.$color-primary, $lightness: 15%);
    }
}

.project-name {
    font-size: vars.$font_size_large;
    font-weight: vars.$font-weight-heading;
    margin: 0;
    margin-right: vars.$margin-small;
}

.project-status {
    display: inline-block;
    padding: 0.25em 0.6em;
    font-size: vars.$font_size_small;
    font-weight: vars.$font-weight-heading;
    border-radius: vars.$border-radius-standard;
    text-transform: capitalize;
    white-space: nowrap;
    flex-shrink: 0;

    &.status-active {
        background-color: color.adjust(vars.$color-primary, $alpha: -0.7);
        color: color.adjust(vars.$color-primary, $lightness: 25%);
    }
    &.status-archived {
        background-color: color.adjust(vars.$color-secondary, $alpha: -0.7);
        color: color.adjust(vars.$color-secondary, $lightness: 25%);
    }
    &.status-read_only {
        background-color: color.adjust(vars.$color-warning, $alpha: -0.7);
        color: color.adjust(vars.$color-warning, $lightness: 15%);
    }
}

.project-description {
    font-size: vars.$font_size_medium;
    margin: 0;
    flex-grow: 1;
}

.card-footer {
    display: flex;
    justify-content: space-between;
    align-items: center;
    font-size: vars.$font_size-small;
    text-transform: capitalize;
}
</style>