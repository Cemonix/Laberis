<template>
    <Card class="data-source-card">
        <template #header>
            <h3 class="data-source-name">{{ dataSource.name }}</h3>
            <span class="asset-count">{{ dataSource.assetCount }} Assets</span>
        </template>

        <p class="data-source-description">
            {{ dataSource.description || "No description provided." }}
        </p>

        <template #footer>
            <div class="card-meta">
                <span>Type: {{ dataSource.type.replace('_', ' ') }}</span>
                <span>Created: {{ formattedDate }}</span>
            </div>
            <div class="card-actions">
                <router-link :to="explorerUrl" custom v-slot="{ navigate }">
                    <Button @click="navigate" variant="secondary" title="View assets in the Dataset Explorer">
                        View Assets
                    </Button>
                </router-link>
                <Button variant="secondary">Import</Button>
                <Button variant="secondary">Export</Button>
            </div>
        </template>
    </Card>
</template>

<script setup lang="ts">
import { computed } from "vue";
import type { DataSource } from "@/types/project/dataSource";
import Card from "@/components/common/Card.vue";
import Button from "@/components/common/Button.vue";

const props = defineProps<{
    dataSource: DataSource;
}>();

const explorerUrl = computed(() => `/projects/${props.dataSource.projectId}/data-sources/${props.dataSource.dataSourceId}`);

const formattedDate = computed(() => {
    return new Date(props.dataSource.createdAt).toLocaleDateString("en-US", {
        year: "numeric",
        month: "short",
        day: "numeric",
    });
});
</script>

<style lang="scss" scoped>
@use "@/styles/variables" as vars;

.data-source-name {
    font-size: vars.$font_size_large;
}

.asset-count {
    font-size: vars.$font_size_small;
    font-weight: bold;
    color: vars.$theme-text-light;
    background-color: vars.$color-gray-200;
    padding: 0.2em 0.6em;
    border-radius: 1em;
}

.data-source-description {
    font-size: vars.$font_size_medium;
    margin: 0;
    flex-grow: 1;
}

.card-meta {
    display: flex;
    flex-direction: column;
    gap: vars.$gap-tiny;
    text-transform: capitalize;
}

.card-actions {
    display: flex;
    gap: vars.$gap-small;
}
</style>