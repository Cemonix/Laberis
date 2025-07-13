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
import type {Project} from "@/types/project/project";
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

<style scoped>
.project-card-link {
    display: block;
    text-decoration: none;
    color: inherit;
    height: 100%;

    &:hover .project-card-content {
        transform: translateY(-5px);
        box-shadow: 0 1px 3px rgba(var(--color-black), 0.1);
        border-color: var(--color-primary);
    }
}

.project-card-content {
    transition: transform 0.2s ease-in-out, box-shadow 0.2s ease-in-out;
}

.project-name {
    font-size: 1.25rem;
    font-weight: 700;
    margin: 0;
    margin-right: 0.5rem;
}

.project-status {
    display: inline-block;
    padding: 0.25em 0.6em;
    font-size: 0.875rem;
    font-weight: 700;
    border-radius: 4px;
    text-transform: capitalize;
    white-space: nowrap;
    flex-shrink: 0;

    &.status-active {
        background-color: var(--color-primary, $alpha: - 0.7);
        color: var(--color-primary);
    }
    &.status-archived {
        background-color: var(--color-secondary, $alpha: - 0.7);
        color: var(--color-secondary);
    }
    &.status-read_only {
        background-color: var(--color-warning, $alpha: - 0.7);
        color: var(--color-warning);
    }
}

.project-description {
    font-size: 1rem;
    margin: 0;
    flex-grow: 1;
}
</style>
