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
                Main Canvas Area (Project: {{ projectId }}, Asset:
                {{ assetId }})
                <p>
                    This page uses the WorkspaceLayout, so no global navbar or
                    footer should be visible.
                </p>
            </div>
            <div class="workspace-annotations-right">
                Right Annotations/Issues Panel
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { defineProps, onMounted } from "vue";

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

onMounted(() => {
    console.log("Annotation Workspace Mounted");
    console.log("Project ID:", props.projectId);
    console.log("Asset ID:", props.assetId);
});
</script>

<style scoped>
.annotation-workspace-container {
    display: flex;
    flex-direction: column;
    height: 100%;
    width: 100%;
    background-color: #34495e;
    color: white;
}

.workspace-top-bar {
    background-color: #2c3e50;
    padding: 10px;
    text-align: center;
    border-bottom: 1px solid #4a6fa5;
    flex-shrink: 0;
}

.workspace-main-area {
    display: flex;
    flex-grow: 1;
    overflow: hidden;
}

.workspace-tools-left {
    width: 200px;
    background-color: #3b597a;
    padding: 10px;
    border-right: 1px solid #4a6fa5;
    flex-shrink: 0;
    overflow-y: auto;
}

.workspace-canvas-area {
    flex-grow: 1;
    background-color: #283747;
    padding: 20px;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    overflow: auto;
}

.workspace-annotations-right {
    width: 250px;
    background-color: #3b597a;
    padding: 10px;
    border-left: 1px solid #4a6fa5;
    flex-shrink: 0;
    overflow-y: auto;
}
</style>
