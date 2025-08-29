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
import {computed} from "vue";
import type {Project} from "@/services/project/project.types";
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
.project-card-link {
    display: block;
    text-decoration: none;
    color: inherit;
    height: 100%;

    &:hover .project-card-content {
        transform: translateY(-5px);
        box-shadow: 1px 1px 2px rgba(0, 0, 0, 0.3);
        border-color: var(--color-primary);
    }
}

.project-card-content {
    transition: transform 0.2s ease-in-out, box-shadow 0.2s ease-in-out;
}

.project-name {
    font-size: 1.5rem;
    font-weight: 800;
}

.project-status {
    display: inline-block;
    flex-shrink: 0;
    padding: 0.25rem 0.75rem;
    font-size: 0.825rem;
    font-weight: 800;
    border-radius: 0.75rem;
    text-transform: capitalize;
    white-space: nowrap;

    &.status-active {
        background-color: var(--color-primary);
        color: var(--color-primary-light);
    }
    &.status-archived {
        background-color: var(--color-secondary);
        color: var(--color-secondary-light);
    }
    &.status-read_only {
        background-color: var(--color-warning);
        color: var(--color-warning-light);
    }
}

.project-description {
    font-size: 1rem;
    margin: 0;
    flex-grow: 1;
}
</style>
