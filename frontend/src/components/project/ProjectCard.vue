<template>
    <router-link :to="projectUrl" class="project-card">
        <header class="card-header">
            <h3 class="project-name">{{ project.name }}</h3>
            <span class="project-status" :class="statusClass">{{ project.status }}</span>
        </header>
        <p class="project-description">{{ project.description || 'No description provided.' }}</p>
        <footer class="card-footer">
            <span class="project-meta">{{ project.projectType.replace('_', ' ') }}</span>
            <span class="project-meta">Created: {{ formattedDate }}</span>
        </footer>
    </router-link>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { Project } from '@/types/project';

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
@use "@/styles/variables.scss" as vars;

.project-card {
    display: flex;
    flex-direction: column;
    background-color: vars.$workspace-container-bg;
    border: 1px solid vars.$workspace-border-color;
    border-radius: vars.$border-radius-standard;
    padding: vars.$padding-medium;
    text-decoration: none;
    color: vars.$workspace-container-text;
    transition: transform vars.$transition-normal, box-shadow vars.$transition-normal;
    height: 100%;

    &:hover {
        transform: translateY(-5px);
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.2);
        border-color: color.adjust(vars.$primary-blue, $lightness: 15%);
    }
}

.card-header {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    margin-bottom: vars.$padding-small;
}

.project-name {
    font-size: vars.$font_size_large;
    font-weight: bold;
    color: vars.$workspace-container-text;
    margin: 0;
    margin-right: vars.$padding-small;
}

.project-status {
    display: inline-block;
    padding: 0.25em 0.6em;
    font-size: vars.$font-size-small;
    font-weight: bold;
    border-radius: vars.$border-radius-standard;
    text-transform: capitalize;
    white-space: nowrap;
    flex-shrink: 0;

    // Status-specific colors
    &.status-active {
        background-color: color.adjust(vars.$primary-blue, $alpha: -0.7);
        color: color.adjust(vars.$primary-blue, $lightness: 25%);
    }
    &.status-archived {
        background-color: color.adjust(#6c757d, $alpha: -0.7);
        color: color.adjust(#6c757d, $lightness: 25%);
    }
    &.status-read_only {
        background-color: color.adjust(#ffc107, $alpha: -0.7);
        color: color.adjust(#ffc107, $lightness: 15%);
    }
}

.project-description {
    font-size: vars.$font_size_medium;
    margin-bottom: vars.$padding-medium;
    flex-grow: 1;
    color: lighten(vars.$workspace-container-text, 25%);
}

.card-footer {
    display: flex;
    justify-content: space-between;
    font-size: vars.$font_size-small;
    color: lighten(vars.$workspace-container-text, 40%);
    border-top: 1px solid vars.$workspace-border-color;
    padding-top: vars.$padding-small;
    text-transform: capitalize;
}
</style>