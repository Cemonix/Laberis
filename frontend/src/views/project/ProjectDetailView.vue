<template>
    <div v-if="project" class="page-container">
        <header class="project-header">
            <h1 class="project-name">{{ project.name }}</h1>
            <p class="project-description">{{ project.description }}</p>
        </header>

        <nav class="project-sub-nav">
            <router-link :to="`/projects/${projectId}`" class="sub-nav-link" exact-active-class="is-active">Dashboard</router-link>
            <router-link :to="`/projects/${projectId}/workflows`" class="sub-nav-link" active-class="is-active">Workflows</router-link>
            <router-link :to="`/projects/${projectId}/assets`" class="sub-nav-link" active-class="is-active">Assets</router-link>
            <router-link :to="`/projects/${projectId}/label-schemes`" class="sub-nav-link" active-class="is-active">Label Schemes</router-link>
            <router-link :to="`/projects/${projectId}/settings`" class="sub-nav-link" active-class="is-active">Settings</router-link>
        </nav>

        <router-view v-slot="{ Component }">
            <transition name="fade-slide" mode="out-in">
                <component :is="Component" />
            </transition>
        </router-view>
    </div>
    <div v-else class="page-container">
        <p>Loading project details or project not found...</p>
    </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import type { Project } from '@/types/project';
import { ProjectStatus, ProjectType } from '@/types/project';

const route = useRoute();
const project = ref<Project | null>(null);
const projectId = route.params.projectId as string;

const MOCK_PROJECTS: Project[] = [
    { projectId: 1, name: 'Urban Object Detection', description: 'Detecting cars, pedestrians, and traffic signs in city environments.', projectType: ProjectType.OBJECT_DETECTION, status: ProjectStatus.ACTIVE, createdAt: '2024-05-15T10:00:00Z', updatedAt: '2024-06-03T14:30:00Z' },
    { projectId: 2, name: 'Medical Image Segmentation', description: 'Segmenting tumors in MRI scans.', projectType: ProjectType.IMAGE_SEGMENTATION, status: ProjectStatus.ACTIVE, createdAt: '2024-03-20T09:00:00Z', updatedAt: '2024-05-28T11:00:00Z' },
    { projectId: 3, name: 'Sentiment Analysis for Reviews', projectType: ProjectType.TEXT_ANNOTATION, status: ProjectStatus.READ_ONLY, createdAt: '2023-11-10T12:00:00Z', updatedAt: '2024-01-25T16:45:00Z' },
    { projectId: 4, name: 'Legacy Project - Archived', description: 'Old project for data classification.', projectType: ProjectType.IMAGE_CLASSIFICATION, status: ProjectStatus.ARCHIVED, createdAt: '2022-01-15T18:00:00Z', updatedAt: '2022-09-30T17:00:00Z' },
];

onMounted(() => {
    const foundProject = MOCK_PROJECTS.find(p => p.projectId.toString() === projectId);
    if (foundProject) {
        project.value = foundProject;
    }
});
</script>

<style lang="scss" scoped>
@use "@/styles/variables.scss" as vars;

.page-container {
    padding: vars.$padding-large;
    width: 100%;
    max-width: 1400px;
    margin: 0 auto;
}

.project-header {
    margin-bottom: vars.$padding-medium;
    .project-name {
        font-size: 2.5rem;
        color: vars.$text_color;
    }
    .project-description {
        font-size: vars.$font_size_large;
        color: lighten(vars.$text_color, 25%);
        margin-top: vars.$padding-small;
    }
}

.project-sub-nav {
    display: flex;
    gap: vars.$padding-small;
    border-bottom: 1px solid vars.$medium-grey-border;
    margin-bottom: vars.$padding-large * 2;
}

.sub-nav-link {
    padding: vars.$padding-small vars.$padding-medium;
    text-decoration: none;
    color: lighten(vars.$text_color, 30%);
    border-bottom: 3px solid transparent;
    transition: color vars.$transition-normal, border-color vars.$transition-normal;

    &:hover {
        color: vars.$text_color;
    }

    &.is-active {
        color: vars.$primary-blue;
        border-bottom-color: vars.$primary-blue;
    }
}

.fade-slide-enter-active,
.fade-slide-leave-active {
    transition: opacity 0.3s ease, transform 0.3s ease;
}

.fade-slide-enter-from,
.fade-slide-leave-to {
    opacity: 0;
    transform: translateY(20px);
}
</style>