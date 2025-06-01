<template>
    <div class="annotation-workspace-container">
        <div class="workspace-top-bar">
            <a href="/home">Home</a>
            Annotation Workspace Top Bar (e.g., Undo, Redo, Asset:
            {{ assetId }})
        </div>
        <div class="workspace-main-area">
            <div class="workspace-tools-left">Left Tools Panel</div>
            <div class="workspace-canvas-area">
                <AnnotationCanvas :image-url="currentImageUrl" />
            </div>
            <div class="workspace-annotations-right">
                Right Annotations/Issues Panel
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { defineProps, onMounted, ref } from "vue";
import AnnotationCanvas from "@/components/workspace/AnnotationCanvas.vue";

const props = defineProps({
    projectId: {
        type: String,
        required: true,
    },
    assetId: {
        type: String,
        required: true,
    },
});

// Temporary image URL for testing
// Replace with a direct link to an image that allows CORS if needed
const currentImageUrl = ref<string | null>(null);

onMounted(() => {
    console.log("Annotation Workspace Mounted");
    console.log("Project ID:", props.projectId);
    console.log("Asset ID:", props.assetId);

    currentImageUrl.value = `https://picsum.photos/seed/${props.assetId}/800/600`;

    // TODO: Later, this will be driven by the Pinia store (Step 3.4.1)
    // Example:
    // import { useWorkspaceStore } from '@/stores/workspaceStore';
    // const workspaceStore = useWorkspaceStore();
    // workspaceStore.loadAsset(props.projectId, props.assetId);
    // currentImageUrl.value = computed(() => workspaceStore.currentImageUrl);
});
</script>

<style lang="scss" scoped>
@use "@/styles/variables.scss" as vars;

.annotation-workspace-container {
    display: flex;
    flex-direction: column;
    height: 100%;
    width: 100%;
    background-color: vars.$workspace-container-bg;
    color: vars.$workspace-container-text;
}

.workspace-top-bar {
    background-color: vars.$workspace-top-bar-bg;
    padding: vars.$padding-small;
    text-align: center;
    border-bottom: 1px solid vars.$workspace-border-color;
    flex-shrink: 0;

    a {
        color: vars.$workspace-container-text;
        margin-right: vars.$padding-medium;
        &:hover {
            text-decoration: underline;
        }
    }
}

.workspace-main-area {
    display: flex;
    flex-grow: 1;
    overflow: hidden;
}

.workspace-tools-left {
    width: 200px;
    background-color: vars.$workspace-panel-bg;
    padding: vars.$padding-small;
    border-right: 1px solid vars.$workspace-border-color;
    flex-shrink: 0;
    overflow-y: auto;
}

.workspace-canvas-area {
  flex-grow: 1;
  display: flex;
  overflow: hidden;
  position: relative;
  padding: vars.$padding-medium;
  background-color: vars.$workspace-layout-bg;
}

.workspace-annotations-right {
    width: 250px;
    background-color: vars.$workspace-panel-bg;
    padding: vars.$padding-small;
    border-left: 1px solid vars.$workspace-border-color;
    flex-shrink: 0;
    overflow-y: auto;
}
</style>
