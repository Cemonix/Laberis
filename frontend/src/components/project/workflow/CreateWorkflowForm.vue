<template>
    <Form @submit="handleSubmit" class="create-workflow-form">
        <div class="form-section">
            <h3 class="section-title">Basic Information</h3>
            
            <div class="form-group">
                <label for="workflow-name">Workflow Name</label>
                <input
                    id="workflow-name"
                    v-model="form.name"
                    type="text"
                    placeholder="Enter workflow name"
                    required
                    maxlength="255"
                    :disabled="isLoading"
                />
                <div v-if="errors.name" class="field-error">{{ errors.name }}</div>
            </div>

            <div class="form-group">
                <label for="workflow-description">Description (Optional)</label>
                <textarea
                    id="workflow-description"
                    v-model="form.description"
                    placeholder="Enter workflow description (optional)"
                    rows="3"
                    maxlength="1000"
                    :disabled="isLoading"
                ></textarea>
                <div class="field-help">Describe the purpose and scope of this workflow</div>
            </div>
        </div>

        <div class="form-section">
            <h3 class="section-title">Workflow Configuration</h3>
            
            <div class="form-group">
                <div class="checkbox-group">
                    <input
                        id="include-revision"
                        v-model="form.includeRevision"
                        type="checkbox"
                        :disabled="isLoading"
                    />
                    <label for="include-revision" class="checkbox-label">
                        Include Revision Stage
                        <span class="checkbox-help">Add a review/revision step between annotation and completion</span>
                    </label>
                </div>
            </div>
        </div>

        <div class="form-section">
            <h3 class="section-title">Stage Assignments</h3>
            <div class="field-help section-help">Assign team members to specific workflow stages. You can modify these assignments later.</div>
            
            <div class="stages-configuration">
                <!-- Annotation Stage -->
                <div class="stage-assignment">
                    <div class="stage-header">
                        <font-awesome-icon :icon="faPenNib" class="stage-icon annotation" />
                        <h4>Annotation Stage</h4>
                        <span class="stage-badge annotation">Required</span>
                    </div>
                    <div class="form-group">
                        <label>Assigned Members</label>
                        <div class="member-selector">
                            <div v-if="loadingMembers" class="loading-text">Loading team members...</div>
                            <div v-else-if="memberError" class="error-text">{{ memberError }}</div>
                            <div v-else>
                                <select 
                                    multiple 
                                    :disabled="isLoading"
                                    v-model="form.annotationMembers"
                                    class="member-select"
                                >
                                    <option 
                                        v-for="member in projectMembers" 
                                        :key="member.id"
                                        :value="member.id"
                                    >
                                        {{ member.userName || member.email }} ({{ member.role }})
                                    </option>
                                </select>
                                <div class="field-help">Hold Ctrl/Cmd to select multiple members</div>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Revision Stage (conditional) -->
                <div v-if="form.includeRevision" class="stage-assignment">
                    <div class="stage-header">
                        <font-awesome-icon :icon="faSearch" class="stage-icon revision" />
                        <h4>Revision Stage</h4>
                        <span class="stage-badge revision">Optional</span>
                    </div>
                    <div class="form-group">
                        <label>Assigned Members</label>
                        <div class="member-selector">
                            <div v-if="loadingMembers" class="loading-text">Loading team members...</div>
                            <div v-else-if="memberError" class="error-text">{{ memberError }}</div>
                            <div v-else>
                                <select 
                                    multiple 
                                    :disabled="isLoading"
                                    v-model="form.revisionMembers"
                                    class="member-select"
                                >
                                    <option 
                                        v-for="member in projectMembers" 
                                        :key="member.id"
                                        :value="member.id"
                                    >
                                        {{ member.userName || member.email }} ({{ member.role }})
                                    </option>
                                </select>
                                <div class="field-help">Hold Ctrl/Cmd to select multiple members</div>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Completion Stage -->
                <div class="stage-assignment">
                    <div class="stage-header">
                        <font-awesome-icon :icon="faCheckCircle" class="stage-icon completion" />
                        <h4>Completion Stage</h4>
                        <span class="stage-badge completion">Required</span>
                    </div>
                    <div class="form-group">
                        <label>Assigned Members</label>
                        <div class="member-selector">
                            <div v-if="loadingMembers" class="loading-text">Loading team members...</div>
                            <div v-else-if="memberError" class="error-text">{{ memberError }}</div>
                            <div v-else>
                                <select 
                                    multiple 
                                    :disabled="isLoading"
                                    v-model="form.completionMembers"
                                    class="member-select"
                                >
                                    <option 
                                        v-for="member in projectMembers" 
                                        :key="member.id"
                                        :value="member.id"
                                    >
                                        {{ member.userName || member.email }} ({{ member.role }})
                                    </option>
                                </select>
                                <div class="field-help">Hold Ctrl/Cmd to select multiple members</div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <div class="form-actions">
            <Button
                type="button"
                variant="secondary"
                @click="$emit('cancel')"
                :disabled="isLoading"
            >
                Cancel
            </Button>
            <Button
                type="submit"
                variant="primary"
                :loading="isLoading"
                :disabled="!isFormValid"
            >
                Create Workflow
            </Button>
        </div>
    </Form>
</template>

<script setup lang="ts">
import {computed, onMounted, reactive, ref} from 'vue';
import {FontAwesomeIcon} from '@fortawesome/vue-fontawesome';
import {faCheckCircle, faPenNib, faSearch} from '@fortawesome/free-solid-svg-icons';
import Form from '@/components/common/Form.vue';
import Button from '@/components/common/Button.vue';
import type {CreateWorkflowWithStagesRequest} from '@/types/workflow';
import {WorkflowStageType} from '@/types/workflow';
import type {ProjectMember} from '@/types/projectMember';
import {projectMemberService} from '@/services/api/projects';
import {AppLogger} from '@/utils/logger';

// TODO: Remove this component. It is no longer used in the application.

const logger = AppLogger.createComponentLogger('CreateWorkflowForm');

interface WorkflowForm {
    name: string;
    description?: string;
    includeRevision: boolean;
    annotationMembers: number[];
    revisionMembers: number[];
    completionMembers: number[];
}

interface Props {
    projectId: number;
}

interface Emits {
    (e: 'save', data: CreateWorkflowWithStagesRequest): void;
    (e: 'cancel'): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

const isLoading = ref(false);
const loadingMembers = ref(false);
const memberError = ref<string | null>(null);
const projectMembers = ref<ProjectMember[]>([]);

const form = reactive<WorkflowForm>({
    name: '',
    description: '',
    includeRevision: false,
    annotationMembers: [],
    revisionMembers: [],
    completionMembers: []
});

const errors = reactive({
    name: ''
});

const isFormValid = computed(() => {
    return form.name.trim().length >= 3 && !errors.name;
});

// Load project members on mount
onMounted(async () => {
    await loadProjectMembers();
});

const loadProjectMembers = async () => {
    loadingMembers.value = true;
    memberError.value = null;
    
    try {
        const members = await projectMemberService.getProjectMembers(props.projectId);
        projectMembers.value = members.filter(member => member.joinedAt); // Only include members who have joined
    } catch (error) {
        memberError.value = 'Failed to load team members';
        logger.error('Error loading project members:', error);
    } finally {
        loadingMembers.value = false;
    }
};

const validateForm = (): boolean => {
    errors.name = '';
    
    if (!form.name.trim()) {
        errors.name = 'Workflow name is required';
        return false;
    }
    
    if (form.name.trim().length < 3) {
        errors.name = 'Workflow name must be at least 3 characters long';
        return false;
    }
    
    if (form.name.trim().length > 255) {
        errors.name = 'Workflow name must be less than 255 characters';
        return false;
    }
    
    return true;
};

const handleSubmit = async () => {
    if (!validateForm()) {
        return;
    }
    
    isLoading.value = true;
    
    try {
        // Create stages array based on form data
        const stages = [];
        
        // Always add annotation stage
        stages.push({
            name: 'Annotation',
            description: 'Initial annotation stage',
            stageOrder: 1,
            stageType: WorkflowStageType.ANNOTATION,
            isInitialStage: true,
            isFinalStage: false,
            assignedProjectMemberIds: form.annotationMembers
        });
        
        // Add revision stage if selected
        if (form.includeRevision) {
            stages.push({
                name: 'Revision',
                description: 'Review and revision stage',
                stageOrder: 2,
                stageType: WorkflowStageType.REVISION,
                isInitialStage: false,
                isFinalStage: false,
                assignedProjectMemberIds: form.revisionMembers
            });
        }
        
        // Always add completion stage
        stages.push({
            name: 'Completion',
            description: 'Final completion stage',
            stageOrder: form.includeRevision ? 3 : 2,
            stageType: WorkflowStageType.COMPLETION,
            isInitialStage: false,
            isFinalStage: true,
            assignedProjectMemberIds: form.completionMembers
        });
        
        const formData: CreateWorkflowWithStagesRequest = {
            name: form.name.trim(),
            description: form.description?.trim() || undefined,
            stages,
            createDefaultStages: false,
            includeReviewStage: form.includeRevision,
        };
        
        emit('save', formData);
    } catch (error) {
        logger.error('Error preparing form data:', error);
    } finally {
        isLoading.value = false;
    }
};

// Reset form when component is mounted or reset
const resetForm = () => {
    form.name = '';
    form.description = '';
    form.includeRevision = false;
    form.annotationMembers = [];
    form.revisionMembers = [];
    form.completionMembers = [];
    errors.name = '';
};

// Expose reset function if needed
defineExpose({ resetForm });
</script>

<style scoped>
.create-workflow-form {
    max-width: 600px;
    margin: 0 auto;
    max-height: 80vh;
    overflow-y: auto;
    padding: 1rem;
}

.form-section {
    margin-bottom: 1.5rem;
    padding: 1rem;
    background: var(--color-white);
    border: 1px solid var(--color-gray-400);
    border-radius: 8px;
    
    &:first-child {
        margin-top: 0;
    }
}

.section-title {
    margin: 0 0 1rem;
    font-size: 1rem;
    font-weight: 600;
    border-bottom: 2px solid var(--color-primary);
    padding-bottom: 0.25rem;
}

.section-help {
    margin-bottom: 1rem !important;
    font-style: italic;
}

.checkbox-group {
    display: flex;
    align-items: flex-start;
    gap: 0.5rem;
    
    input[type="checkbox"] {
        margin-top: 2px;
    }
}

.checkbox-label {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
    cursor: pointer;
    font-weight: 500;
}

.checkbox-help {
    font-size: 0.875rem;
    color: var(--color-gray-600);
    font-weight: 400;
}

.stages-configuration {
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.stage-assignment {
    padding: 1rem;
    background: var(--color-gray-50);
    border-radius: 4px;
    border: 1px solid var(--color-gray-400);
}

.stage-header {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    margin-bottom: 0.5rem;
    
    h4 {
        margin: 0;
        flex-grow: 1;
        color: var(--color-gray-800);
        font-size: 0.875rem;
        font-weight: 600;
    }
}

.stage-icon {
    font-size: 1rem;
    
    &.annotation {
        color: var(--color-info);
    }
    
    &.revision {
        color: var(--color-warning-dark);
    }
    
    &.completion {
        color: var(--color-success);
    }
}

.stage-badge {
    padding: 1px 6px;
    border-radius: 2px;
    font-size: 0.75rem;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 1px;
    
    &.annotation {
        background: var(--color-info-light);
        color: var(--color-info);
        border: 1px solid var(--color-info);
    }
    
    &.revision {
        background: var(--color-warning-light);
        color: var(--color-warning-dark);
        border: 1px solid var(--color-warning);
    }
    
    &.completion {
        background: var(--color-success-light);
        color: var(--color-success);
        border: 1px solid var(--color-success);
    }
}

.member-selector {
    .loading-text, .error-text {
        padding: 0.5rem;
        text-align: center;
        font-style: italic;
    }
    
    .loading-text {
        color: var(--color-gray-600);
    }
    
    .error-text {
        color: var(--color-error);
    }
}

.member-select {
    width: 100%;
    min-height: 100px;
    padding: 0.5rem;
    border: 1px solid var(--color-gray-400);
    border-radius: 4px;
    background: var(--color-white);
    font-size: 0.875rem;
    color: var(--color-gray-800);
    
    &:focus {
        outline: none;
        border-color: var(--color-primary);
        box-shadow: 0 0 0 2px rgba(0, 123, 255, 0.25);
    }
    
    &:disabled {
        background: var(--color-gray-100);
        color: var(--color-gray-600);
        cursor: not-allowed;
    }
    
    option {
        padding: 0.25rem;
        
        &:checked {
            background: var(--color-primary);
            color: var(--color-white);
        }
    }
}

.field-error {
    color: var(--color-error);
    font-size: 0.875rem;
    font-weight: 500;
    margin-top: 0.25rem;
}

.field-help {
    color: var(--color-gray-600);
    font-size: 0.875rem;
    line-height: 1.4;
    margin-top: 0.25rem;
}

.form-actions {
    display: flex;
    justify-content: flex-end;
    position: relative;
    background: var(--color-white);
    margin-left: -1rem;
    margin-right: -1rem;
    margin-bottom: -1rem;
    padding-left: 1rem;
    padding-right: 1rem;
    padding-bottom: 1rem;
}

@media (max-width: 768px) {
    .create-workflow-form {
        max-width: 100%;
        max-height: 85vh;
        padding: 0.5rem;
    }
    
    .form-section {
        padding: 0.5rem;
    }
    
    .member-select {
        min-height: 80px;
    }
    
    .form-actions {
        flex-direction: column;
        margin-left: -0.5rem;
        margin-right: -0.5rem;
        margin-bottom: -0.5rem;
        padding-left: 0.5rem;
        padding-right: 0.5rem;
        padding-bottom: 0.5rem;
        
        button {
            width: 100%;
        }
    }
}
</style>
