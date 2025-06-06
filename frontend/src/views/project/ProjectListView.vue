<template>
    <div class="page-container">
        <h1 class="page-title">Projects</h1>
        <div class="project-grid">
            <ProjectCard
                v-for="project in projects"
                :key="project.projectId"
                :project="project"
            />
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import ProjectCard from '@/components/project/ProjectCard.vue';
import type { Project } from '@/types/project';
import { ProjectStatus, ProjectType } from '@/types/project';

const projects = ref<Project[]>([]);

// TODO: Replace with actual API call to fetch projects
onMounted(() => {
    projects.value = [
        {
            projectId: 1,
            name: 'Urban Object Detection',
            description: 'Detecting cars, pedestrians, and traffic signs in city environments.',
            projectType: ProjectType.OBJECT_DETECTION,
            status: ProjectStatus.ACTIVE,
            createdAt: '2024-05-15T10:00:00Z',
            updatedAt: '2024-06-03T14:30:00Z',
        },
        {
            projectId: 2,
            name: 'Medical Image Segmentation',
            description: 'Segmenting tumors in MRI scans.',
            projectType: ProjectType.IMAGE_SEGMENTATION,
            status: ProjectStatus.ACTIVE,
            createdAt: '2024-03-20T09:00:00Z',
            updatedAt: '2024-05-28T11:00:00Z',
        },
        {
            projectId: 3,
            name: 'Sentiment Analysis for Reviews',
            projectType: ProjectType.TEXT_ANNOTATION,
            status: ProjectStatus.READ_ONLY,
            createdAt: '2023-11-10T12:00:00Z',
            updatedAt: '2024-01-25T16:45:00Z',
        },
        {
            projectId: 4,
            name: 'Legacy Project - Archived',
            description: 'Old project for data classification.',
            projectType: ProjectType.IMAGE_CLASSIFICATION,
            status: ProjectStatus.ARCHIVED,
            createdAt: '2022-01-15T18:00:00Z',
            updatedAt: '2022-09-30T17:00:00Z',
        },
    ];
});
</script>

<style lang="scss" scoped>
@use "@/styles/variables.scss" as vars;

.page-container {
    padding: vars.$padding-large;
    width: 100%;
    max-width: 1200px;
    margin: 0 auto;
}

.page-title {
    font-size: vars.$font-size-xxlarge;
    margin-bottom: vars.$padding-large;
    color: vars.$text_color;
}

.project-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
    gap: vars.$padding-large;
}
</style>