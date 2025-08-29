<template>
    <div class="deletion-impact-dialog">
        <div class="warning-header">
            <h3>⚠️ Delete Label Scheme: {{ impact.labelSchemeName }}</h3>
            <p>This action will soft-delete the label scheme and all its labels. Existing annotations will be preserved but no new annotations can be created with these labels.</p>
        </div>

        <div class="impact-summary">
            <div class="impact-stats">
                <div class="stat-item">
                    <span class="stat-number">{{ impact.totalLabelsCount }}</span>
                    <span class="stat-label">Labels</span>
                </div>
                <div class="stat-item">
                    <span class="stat-number">{{ impact.totalAnnotationsCount }}</span>
                    <span class="stat-label">Annotations</span>
                </div>
            </div>
        </div>

        <div v-if="impact.labelImpacts.length > 0" class="labels-impact">
            <h4>Affected Labels:</h4>
            <div class="labels-list">
                <div 
                    v-for="labelImpact in impact.labelImpacts" 
                    :key="labelImpact.labelId"
                    class="label-impact-item"
                >
                    <div class="label-info">
                        <div 
                            class="label-color" 
                            :style="{ backgroundColor: labelImpact.labelColor || '#ccc' }"
                        ></div>
                        <span class="label-name">{{ labelImpact.labelName }}</span>
                    </div>
                    <span class="annotation-count">{{ labelImpact.annotationsCount }} annotations</span>
                </div>
            </div>
        </div>

        <div class="safety-note">
            <p><strong>Safety Notice:</strong> This is a soft delete operation. You can reactivate this scheme later if needed. All existing annotations will remain intact and viewable.</p>
        </div>

        <div class="action-buttons">
            <Button variant="secondary" @click="$emit('cancel')" :disabled="isLoading">
                Cancel
            </Button>
            <Button variant="danger" @click="$emit('confirm')" :disabled="isLoading">
                {{ isLoading ? 'Deleting...' : 'Delete Label Scheme' }}
            </Button>
        </div>
    </div>
</template>

<script setup lang="ts">
import type { LabelSchemeDeletionImpact } from '@/services/project/labelScheme/label.types';
import Button from '@/components/common/Button.vue';

defineProps<{
    impact: LabelSchemeDeletionImpact;
    isLoading?: boolean;
}>();

defineEmits<{
    confirm: [];
    cancel: [];
}>();
</script>

<style scoped>
.deletion-impact-dialog {
    max-width: 500px;
    padding: 1rem;
}

.warning-header {
    margin-bottom: 1.5rem;
}

.warning-header h3 {
    color: var(--color-red-600);
    margin-bottom: 0.5rem;
    font-size: 1.25rem;
}

.warning-header p {
    color: var(--color-gray-700);
    line-height: 1.5;
}

.impact-summary {
    background-color: var(--color-red-50);
    border: 1px solid var(--color-red-200);
    border-radius: 0.5rem;
    padding: 1rem;
    margin-bottom: 1.5rem;
}

.impact-stats {
    display: flex;
    gap: 2rem;
    justify-content: center;
}

.stat-item {
    display: flex;
    flex-direction: column;
    align-items: center;
    text-align: center;
}

.stat-number {
    font-size: 2rem;
    font-weight: bold;
    color: var(--color-red-600);
}

.stat-label {
    font-size: 0.875rem;
    color: var(--color-gray-600);
    text-transform: uppercase;
    letter-spacing: 0.05em;
}

.labels-impact {
    margin-bottom: 1.5rem;
}

.labels-impact h4 {
    margin-bottom: 0.75rem;
    color: var(--color-gray-800);
}

.labels-list {
    max-height: 200px;
    overflow-y: auto;
    border: 1px solid var(--color-gray-200);
    border-radius: 0.375rem;
    background-color: var(--color-gray-50);
}

.label-impact-item {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 0.75rem;
    border-bottom: 1px solid var(--color-gray-200);
}

.label-impact-item:last-child {
    border-bottom: none;
}

.label-info {
    display: flex;
    align-items: center;
    gap: 0.5rem;
}

.label-color {
    width: 1rem;
    height: 1rem;
    border-radius: 0.25rem;
    border: 1px solid var(--color-gray-300);
}

.label-name {
    font-weight: medium;
}

.annotation-count {
    font-size: 0.875rem;
    color: var(--color-gray-600);
}

.safety-note {
    background-color: var(--color-blue-50);
    border: 1px solid var(--color-blue-200);
    border-radius: 0.375rem;
    padding: 0.75rem;
    margin-bottom: 1.5rem;
}

.safety-note p {
    margin: 0;
    color: var(--color-blue-800);
    font-size: 0.875rem;
    line-height: 1.4;
}

.action-buttons {
    display: flex;
    gap: 0.75rem;
    justify-content: flex-end;
}
</style>