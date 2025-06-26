<template>
    <router-link :to="projectUrl" class="project-card-link">
        <Card class="project-card-content">
            <template #header>
                <h3 class="project-name">{{ project.name }}</h3>
                <span class="project-status" :class="statusClass">{{
                    project.status
                }}</span>
            </template>

            <p class="project-description">
                {{ project.description || "No description provided." }}
            </p>

            <template #footer>
                <span class="project-meta">{{
                    project.projectType.replace("_", " ")
                }}</span>
                <span class="project-meta">Created: {{ formattedDate }}</span>
            </template>
        </Card>
    </router-link>
</template>

<script setup lang="ts">
import { computed } from "vue";
import type { Project } from "@/types/project/project";
import Card from "@/components/common/Card.vue";

const props = defineProps<{
    project: Project;
}>();

const projectUrl = computed(() => `/projects/${props.project.id}`);
const statusClass = computed(
    () => `status-${props.project.status.toLowerCase()}`
);

const formattedDate = computed(() => {
    return new Date(props.project.createdAt).toLocaleDateString("en-US", {
        year: "numeric",
        month: "long",
        day: "numeric",
    });
});
</script>

<style lang="scss" scoped>
@use "sass:color";
@use "@/styles/variables" as vars;

.project-card-link {
    display: block;
    text-decoration: none;
    color: inherit;
    height: 100%;

    &:hover .project-card-content {
        transform: translateY(-5px);
        box-shadow: vars.$shadow-md;
        border-color: color.adjust(vars.$color-primary, $lightness: 15%);
    }
}

.project-card-content {
    transition: transform 0.2s ease-in-out, box-shadow 0.2s ease-in-out;
}

.project-name {
    font-size: vars.$font-size-large;
    font-weight: vars.$font-weight-xlarge;
    margin: 0;
    margin-right: vars.$margin-small;
}

.project-status {
    display: inline-block;
    padding: 0.25em 0.6em;
    font-size: vars.$font-size-small;
    font-weight: vars.$font-weight-xlarge;
    border-radius: vars.$border-radius;
    text-transform: capitalize;
    white-space: nowrap;
    flex-shrink: 0;

    &.status-active {
        background-color: color.adjust(vars.$color-primary, $alpha: -0.7);
        color: color.adjust(vars.$color-primary, $lightness: 10%);
    }
    &.status-archived {
        background-color: color.adjust(vars.$color-secondary, $alpha: -0.7);
        color: color.adjust(vars.$color-secondary, $lightness: 10%);
    }
    &.status-read_only {
        background-color: color.adjust(vars.$color-warning, $alpha: -0.7);
        color: vars.$color-warning;
    }
}

.project-description {
    font-size: vars.$font-size-medium;
    margin: 0;
    flex-grow: 1;
}
</style>
