<template>
    <div class="create-workflow-wizard">
        <Stepper
            :steps="stepperSteps"
            :can-proceed="canProceedToNextStep"
            :loading="isLoading"
            :submit-icon="faPlus"
            submit-button-text="Create Workflow"
            @step-change="handleStepChange"
            @submit="handleSubmit"
            @cancel="$emit('cancel')"
        >
            <template #default="{ currentStep }">
                <!-- Basic Setup -->
                <div v-if="currentStep === 0" class="step-panel">
                    <h3>Basic Workflow Information</h3>
                    <p class="step-intro">Start by giving your workflow a name and decide if you need a revision stage.</p>

                    <div class="form-section">
                        <div class="form-group">
                            <label for="workflow-name">Workflow Name <span class="required">*</span></label>
                            <input
                                id="workflow-name"
                                v-model="form.name"
                                type="text"
                                placeholder="e.g., Medical Image Annotation, Document Review"
                                required
                                maxlength="255"
                                :disabled="isLoading"
                                class="form-input"
                            />
                            <div v-if="errors.name" class="field-error">{{ errors.name }}</div>
                        </div>

                        <div class="form-group">
                            <label for="workflow-description">Description (Optional)</label>
                            <textarea
                                id="workflow-description"
                                v-model="form.description"
                                placeholder="Describe the purpose and scope of this workflow..."
                                rows="3"
                                maxlength="1000"
                                :disabled="isLoading"
                                class="form-textarea"
                            ></textarea>
                            <div class="field-help">Help your team understand what this workflow is for</div>
                        </div>

                        <div class="form-group">
                            <div class="checkbox-wrapper">
                                <input
                                    id="include-revision"
                                    v-model="form.includeRevision"
                                    type="checkbox"
                                    :disabled="isLoading"
                                />
                                <label for="include-revision" class="checkbox-label">
                                    <strong>Include Revision Stage</strong>
                                    <span class="checkbox-description">Add a review step between annotation and completion for quality control</span>
                                </label>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Data Source Configuration -->
                <div v-if="currentStep === 1" class="step-panel">
                    <h3>Data Source Configuration</h3>
                    <p class="step-intro">Choose which data sources each stage will use for input and output.</p>

                    <div v-if="loadingDataSources" class="loading-section">
                        <font-awesome-icon :icon="faSpinner" spin />
                        Loading data sources...
                    </div>

                    <div v-else-if="dataSourceError" class="error-section">
                        <font-awesome-icon :icon="faExclamationTriangle" />
                        {{ dataSourceError }}
                    </div>

                    <div v-else class="data-source-configuration">

                        <!-- Annotation Stage -->
                        <div class="stage-data-source-config">
                            <div class="stage-header">
                                <font-awesome-icon :icon="faPenNib" class="stage-icon annotation" />
                                <h4>Annotation Stage</h4>
                                <span class="stage-badge required">Required</span>
                            </div>

                            <div class="data-source-selectors">
                                <div class="form-group">
                                    <label>Input Data Source <span class="required">*</span></label>
                                    <select 
                                        v-model="form.annotationInputDataSourceId" 
                                        :disabled="isLoading"
                                        class="form-select"
                                        :class="{ 'error': errors.annotationInputDataSourceId }"
                                        required
                                    >
                                        <option value="">Select input data source...</option>
                                        <option 
                                            v-for="dataSource in availableDataSourcesForAnnotation" 
                                            :key="dataSource.id"
                                            :value="dataSource.id"
                                        >
                                            {{ dataSource.name }} ({{ dataSource.assetCount || 0 }} assets)
                                        </option>
                                    </select>
                                    <div v-if="errors.annotationInputDataSourceId" class="field-error">{{ errors.annotationInputDataSourceId }}</div>
                                    <div class="field-help">Choose where annotation tasks will get their input data from</div>
                                </div>
                            </div>
                        </div>

                        <!-- Revision Stage (if enabled) -->
                        <div v-if="form.includeRevision" class="stage-data-source-config">
                            <div class="stage-header">
                                <font-awesome-icon :icon="faSearch" class="stage-icon revision" />
                                <h4>Revision Stage</h4>
                                <span class="stage-badge optional">Optional</span>
                            </div>

                            <div class="data-source-selectors">
                                <div class="form-group">
                                    <label>Input Data Source <span class="required">*</span></label>
                                    <select 
                                        v-model="form.revisionInputDataSourceId" 
                                        :disabled="isLoading"
                                        class="form-select"
                                        :class="{ 'error': errors.revisionInputDataSourceId }"
                                        required
                                    >
                                        <option value="">Select input data source...</option>
                                        <option 
                                            v-for="dataSource in availableDataSourcesForRevision" 
                                            :key="dataSource.id"
                                            :value="dataSource.id"
                                        >
                                            {{ dataSource.name }} ({{ dataSource.assetCount || 0 }} assets)
                                        </option>
                                    </select>
                                    <div v-if="errors.revisionInputDataSourceId" class="field-error">{{ errors.revisionInputDataSourceId }}</div>
                                    <div class="field-help">Revision stage requires its own data source for proper workflow tracking</div>
                                </div>
                            </div>
                        </div>

                        <!-- Completion Stage -->
                        <div class="stage-data-source-config">
                            <div class="stage-header">
                                <font-awesome-icon :icon="faCheckCircle" class="stage-icon completion" />
                                <h4>Completion Stage</h4>
                                <span class="stage-badge required">Required</span>
                            </div>

                            <div class="data-source-selectors">
                                <div class="form-group">
                                    <label>Input Data Source</label>
                                    <select 
                                        v-model="form.completionInputDataSourceId" 
                                        :disabled="isLoading"
                                        class="form-select"
                                    >
                                        <option value="">Use previous stage outputs</option>
                                        <option 
                                            v-for="dataSource in availableDataSourcesForCompletion" 
                                            :key="dataSource.id"
                                            :value="dataSource.id"
                                        >
                                            {{ dataSource.name }} ({{ dataSource.assetCount || 0 }} assets)
                                        </option>
                                    </select>
                                    <div class="field-help">Typically uses outputs from the previous stage</div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Member Assignments -->
                <div v-if="currentStep === 2" class="step-panel">
                    <h3>Member Assignments</h3>
                    <p class="step-intro">Assign team members to each workflow stage.</p>

                    <div v-if="loadingMembers" class="loading-section">
                        <font-awesome-icon :icon="faSpinner" spin />
                        Loading team members...
                    </div>

                    <div v-else-if="memberError" class="error-section">
                        <font-awesome-icon :icon="faExclamationTriangle" />
                        {{ memberError }}
                    </div>

                    <div v-else class="member-assignments">
                        <!-- Workflow Preview -->
                        <div class="workflow-preview">
                            <h4>Workflow Overview</h4>
                            <div class="workflow-stages">
                                <div class="workflow-stage">
                                    <div class="stage-node annotation">
                                        <font-awesome-icon :icon="faPenNib" />
                                        <span>Annotation</span>
                                    </div>
                                    <div class="stage-members">
                                        {{ getSelectedMemberNames(form.annotationMembers).join(', ') || 'No members assigned' }}
                                    </div>
                                </div>

                                <div v-if="form.includeRevision" class="workflow-arrow">
                                    <font-awesome-icon :icon="faArrowRight" />
                                </div>

                                <div v-if="form.includeRevision" class="workflow-stage">
                                    <div class="stage-node revision">
                                        <font-awesome-icon :icon="faSearch" />
                                        <span>Revision</span>
                                    </div>
                                    <div class="stage-members">
                                        {{ getSelectedMemberNames(form.revisionMembers).join(', ') || 'No members assigned' }}
                                    </div>
                                </div>

                                <div class="workflow-arrow">
                                    <font-awesome-icon :icon="faArrowRight" />
                                </div>

                                <div class="workflow-stage">
                                    <div class="stage-node completion">
                                        <font-awesome-icon :icon="faCheckCircle" />
                                        <span>Completion</span>
                                    </div>
                                    <div class="stage-members">
                                        {{ getSelectedMemberNames(form.completionMembers).join(', ') || 'No members assigned' }}
                                    </div>
                                </div>
                            </div>
                        </div>

                        <!-- Member Assignment Sections -->
                        <div class="assignment-sections">
                            <!-- Annotation Stage Assignment -->
                            <div class="stage-assignment">
                                <div class="stage-header">
                                    <font-awesome-icon :icon="faPenNib" class="stage-icon annotation" />
                                    <h4>Annotation Stage</h4>
                                    <span class="stage-badge required">Required</span>
                                </div>
                                <div class="member-selector">
                                    <label>Assigned Members <span class="required">*</span></label>
                                    <div class="member-grid">
                                        <div 
                                            v-for="member in projectMembers" 
                                            :key="member.id"
                                            class="member-card"
                                            :class="{ selected: form.annotationMembers.includes(member.id) }"
                                            @click="toggleMemberAssignment('annotationMembers', member.id)"
                                        >
                                            <input 
                                                type="checkbox" 
                                                :checked="form.annotationMembers.includes(member.id)"
                                                @click.stop
                                                @change="toggleMemberAssignment('annotationMembers', member.id)"
                                            />
                                            <div class="member-info">
                                                <div class="member-name">{{ member.userName || member.email }}</div>
                                                <div class="member-role">{{ member.role }}</div>
                                            </div>
                                        </div>
                                    </div>
                                    <div v-if="errors.annotationMembers" class="field-error">{{ errors.annotationMembers }}</div>
                                </div>
                            </div>

                            <!-- Revision Stage Assignment (if enabled) -->
                            <div v-if="form.includeRevision" class="stage-assignment">
                                <div class="stage-header">
                                    <font-awesome-icon :icon="faSearch" class="stage-icon revision" />
                                    <h4>Revision Stage</h4>
                                    <span class="stage-badge optional">Optional</span>
                                </div>
                                <div class="member-selector">
                                    <label>Assigned Members</label>
                                    <div class="member-grid">
                                        <div 
                                            v-for="member in projectMembers" 
                                            :key="member.id"
                                            class="member-card"
                                            :class="{ selected: form.revisionMembers.includes(member.id) }"
                                            @click="toggleMemberAssignment('revisionMembers', member.id)"
                                        >
                                            <input 
                                                type="checkbox" 
                                                :checked="form.revisionMembers.includes(member.id)"
                                                @click.stop
                                                @change="toggleMemberAssignment('revisionMembers', member.id)"
                                            />
                                            <div class="member-info">
                                                <div class="member-name">{{ member.userName || member.email }}</div>
                                                <div class="member-role">{{ member.role }}</div>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>

                            <!-- Completion Stage Assignment -->
                            <div class="stage-assignment">
                                <div class="stage-header">
                                    <font-awesome-icon :icon="faCheckCircle" class="stage-icon completion" />
                                    <h4>Completion Stage</h4>
                                    <span class="stage-badge required">Required</span>
                                </div>
                                <div class="member-selector">
                                    <label>Assigned Members <span class="required">*</span></label>
                                    <div class="member-grid">
                                        <div 
                                            v-for="member in projectMembers" 
                                            :key="member.id"
                                            class="member-card"
                                            :class="{ selected: form.completionMembers.includes(member.id) }"
                                            @click="toggleMemberAssignment('completionMembers', member.id)"
                                        >
                                            <input 
                                                type="checkbox" 
                                                :checked="form.completionMembers.includes(member.id)"
                                                @click.stop
                                                @change="toggleMemberAssignment('completionMembers', member.id)"
                                            />
                                            <div class="member-info">
                                                <div class="member-name">{{ member.userName || member.email }}</div>
                                                <div class="member-role">{{ member.role }}</div>
                                            </div>
                                        </div>
                                    </div>
                                    <div v-if="errors.completionMembers" class="field-error">{{ errors.completionMembers }}</div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </template>
        </Stepper>
    </div>
</template>

<script setup lang="ts">
import {computed, onMounted, reactive, ref, watch} from 'vue';
import {FontAwesomeIcon} from '@fortawesome/vue-fontawesome';
import {
    faArrowRight,
    faCheckCircle,
    faExclamationTriangle,
    faPenNib,
    faPlus,
    faSearch,
    faSpinner
} from '@fortawesome/free-solid-svg-icons';
import type {StepperStep} from '@/components/common/Stepper.vue';
import Stepper from '@/components/common/Stepper.vue';
import type {CreateWorkflowWithStagesRequest} from '@/types/workflow';
import {WorkflowStageType} from '@/types/workflow';
import type {ProjectMember} from '@/types/projectMember';
import type {DataSource} from '@/types/dataSource';
import {projectMemberService, dataSourceService} from '@/services/api/projects';
import {AppLogger} from '@/utils/logger';
import {useToast} from '@/composables/useToast';

const logger = AppLogger.createComponentLogger('CreateWorkflowWizard');
const { showApiError } = useToast();

// Props
interface Props {
    projectId: number;
}

const props = defineProps<Props>();

// Emits
const emit = defineEmits<{
    submit: [data: CreateWorkflowWithStagesRequest];
    cancel: [];
}>();

// Stepper configuration
const stepperSteps: StepperStep[] = [
    {
        id: 'basic',
        title: 'Basic Setup',
        description: 'Name and configuration',
        validation: () => form.name.trim().length > 0
    },
    {
        id: 'datasources',
        title: 'Data Sources',
        description: 'Configure data flow',
        validation: () => form.annotationInputDataSourceId !== ''
    },
    {
        id: 'members',
        title: 'Assignments',
        description: 'Assign team members',
        validation: () => form.annotationMembers.length > 0 && form.completionMembers.length > 0
    }
];

// State
const isLoading = ref(false);
const loadingMembers = ref(false);
const loadingDataSources = ref(false);
const memberError = ref<string>('');
const dataSourceError = ref<string>('');

const projectMembers = ref<ProjectMember[]>([]);
const availableDataSources = ref<DataSource[]>([]);

// Form data
interface WorkflowWizardForm {
    name: string;
    description?: string;
    includeRevision: boolean;
    annotationInputDataSourceId: number | '';
    revisionInputDataSourceId: number | '';
    completionInputDataSourceId: number | '';
    annotationMembers: number[];
    revisionMembers: number[];
    completionMembers: number[];
}

const form = reactive<WorkflowWizardForm>({
    name: '',
    description: '',
    includeRevision: false,
    annotationInputDataSourceId: '',
    revisionInputDataSourceId: '',
    completionInputDataSourceId: '',
    annotationMembers: [],
    revisionMembers: [],
    completionMembers: []
});

// Validation errors
const errors = reactive({
    name: '',
    annotationInputDataSourceId: '',
    revisionInputDataSourceId: '',
    annotationMembers: '',
    completionMembers: ''
});

// Data source availability for each stage
const availableDataSourcesForAnnotation = computed(() => {
    return availableDataSources.value;
});

const availableDataSourcesForRevision = computed(() => {
    return availableDataSources.value.filter(ds => 
        ds.id !== form.annotationInputDataSourceId &&
        ds.id !== form.completionInputDataSourceId
    );
});

const availableDataSourcesForCompletion = computed(() => {
    return availableDataSources.value.filter(ds => 
        ds.id !== form.annotationInputDataSourceId &&
        ds.id !== form.revisionInputDataSourceId
    );
});

// Data fetching
const fetchProjectMembers = async () => {
    loadingMembers.value = true;
    memberError.value = '';
    try {
        const response = await projectMemberService.getProjectMembers(props.projectId);
        projectMembers.value = response;
        logger.info(`Loaded ${projectMembers.value.length} project members`);
    } catch (error) {
        memberError.value = 'Failed to load team members';
        showApiError(error, 'Failed to load team members');
    } finally {
        loadingMembers.value = false;
    }
};

const fetchDataSources = async () => {
    loadingDataSources.value = true;
    dataSourceError.value = '';
    try {
        const response = await dataSourceService.getDataSources({ 
            projectId: props.projectId,
            pageSize: 100 // Get all data sources
        });
        availableDataSources.value = response.data;
        logger.info(`Loaded ${availableDataSources.value.length} data sources`);
    } catch (error) {
        dataSourceError.value = 'Failed to load data sources';
        showApiError(error, 'Failed to load data sources');
    } finally {
        loadingDataSources.value = false;
    }
};

// Navigation logic
const canProceedToNextStep = computed(() => {
    if (isLoading.value) return false;
    
    // Run validation and check if there are no errors
    validateCurrentStep();
    return hasNoErrors();
});

// Stepper event handlers
const handleStepChange = (currentStep: number, previousStep: number) => {
    logger.debug(`Step changed from ${previousStep} to ${currentStep}`);
    
    // Load data when needed
    if (currentStep === 1 && availableDataSources.value.length === 0) {
        fetchDataSources();
    } else if (currentStep === 2 && projectMembers.value.length === 0) {
        fetchProjectMembers();
    }
};

// Validation
const validateCurrentStep = () => {
    // Clear previous errors
    Object.keys(errors).forEach(key => {
        errors[key as keyof typeof errors] = '';
    });

    // Basic information validation
    if (!form.name.trim()) {
        errors.name = 'Workflow name is required';
        return;
    }

    // Data source validation - enforce 1:1 mapping for non-completion stages
    if (!form.annotationInputDataSourceId) {
        errors.annotationInputDataSourceId = 'Annotation stage must have exactly one input data source';
        return;
    }

    if (form.includeRevision && !form.revisionInputDataSourceId) {
        errors.revisionInputDataSourceId = 'Revision stage must have exactly one input data source';
        return;
    }

    // Member assignment validation
    if (form.annotationMembers.length === 0) {
        errors.annotationMembers = 'At least one member must be assigned to annotation stage';
        return;
    }

    if (form.completionMembers.length === 0) {
        errors.completionMembers = 'At least one member must be assigned to completion stage';
        return;
    }
};

const hasNoErrors = () => {
    return Object.values(errors).every(error => error === '');
};

// Member assignment helpers
const toggleMemberAssignment = (stageKey: 'annotationMembers' | 'revisionMembers' | 'completionMembers', memberId: number) => {
    const members = form[stageKey];
    const index = members.indexOf(memberId);
    if (index > -1) {
        members.splice(index, 1);
    } else {
        members.push(memberId);
    }
};

const getSelectedMemberNames = (memberIds: number[]) => {
    return memberIds
        .map(id => projectMembers.value.find(m => m.id === id))
        .filter(Boolean)
        .map(member => member!.userName || member!.email);
};

// Form submission
const handleSubmit = async () => {
    validateCurrentStep();
    if (!hasNoErrors()) {
        return;
    }

    isLoading.value = true;
    try {
        const stages: CreateWorkflowWithStagesRequest['stages'] = [];

        // Annotation stage
        stages.push({
            name: 'Annotation',
            description: 'Primary annotation stage',
            stageOrder: 1,
            stageType: WorkflowStageType.ANNOTATION,
            isInitialStage: true,
            isFinalStage: !form.includeRevision,
            inputDataSourceId: form.annotationInputDataSourceId as number,
            assignedProjectMemberIds: form.annotationMembers
        });

        // Revision stage (if enabled)
        if (form.includeRevision) {
            stages.push({
                name: 'Revision',
                description: 'Review and revision stage',
                stageOrder: 2,
                stageType: WorkflowStageType.REVISION,
                isInitialStage: false,
                isFinalStage: false,
                inputDataSourceId: form.revisionInputDataSourceId as number || undefined,
                assignedProjectMemberIds: form.revisionMembers
            });
        }

        // Completion stage
        stages.push({
            name: 'Completion',
            description: 'Final completion stage',
            stageOrder: form.includeRevision ? 3 : 2,
            stageType: WorkflowStageType.COMPLETION,
            isInitialStage: false,
            isFinalStage: true,
            inputDataSourceId: form.completionInputDataSourceId as number || undefined,
            assignedProjectMemberIds: form.completionMembers
        });

        const workflowData: CreateWorkflowWithStagesRequest = {
            name: form.name,
            description: form.description || undefined,
            stages,
            createDefaultStages: false,
            includeReviewStage: form.includeRevision
        };

        emit('submit', workflowData);
    } catch (error) {
        showApiError(error, 'Failed to create workflow');
    } finally {
        isLoading.value = false;
    }
};

// Lifecycle
onMounted(() => {
    fetchProjectMembers();
    fetchDataSources();
});

// Watch for data source conflicts and auto-clear conflicting selections
watch(() => form.annotationInputDataSourceId, (newValue) => {
    if (newValue) {
        // Clear revision if it conflicts
        if (form.revisionInputDataSourceId === newValue) {
            form.revisionInputDataSourceId = '';
        }
        // Clear completion if it conflicts
        if (form.completionInputDataSourceId === newValue) {
            form.completionInputDataSourceId = '';
        }
    }
});

watch(() => form.revisionInputDataSourceId, (newValue) => {
    if (newValue) {
        // Clear annotation if it conflicts
        if (form.annotationInputDataSourceId === newValue) {
            form.annotationInputDataSourceId = '';
        }
        // Clear completion if it conflicts
        if (form.completionInputDataSourceId === newValue) {
            form.completionInputDataSourceId = '';
        }
    }
});

watch(() => form.completionInputDataSourceId, (newValue) => {
    if (newValue) {
        // Clear annotation if it conflicts
        if (form.annotationInputDataSourceId === newValue) {
            form.annotationInputDataSourceId = '';
        }
        // Clear revision if it conflicts
        if (form.revisionInputDataSourceId === newValue) {
            form.revisionInputDataSourceId = '';
        }
    }
});
</script>

<style lang="scss" scoped>
.create-workflow-wizard {
    max-width: 100%;
    margin: 0;
}

.stepper-content {
    .step-panel {
        h3 {
            color: var(--color-text-primary);
            font-family: var(--font-family-heading), sans-serif;
            margin-bottom: 0.5rem;
        }
        
        .step-intro {
            color: var(--color-text-secondary);
            margin-bottom: 2rem;
        }
    }
}

.form-section {
    .form-group {
        margin-bottom: 1.5rem;
        
        label {
            display: block;
            font-weight: 500;
            font-family: var(--font-family-body), sans-serif;
            color: var(--color-gray-700);
            margin-bottom: 0.5rem;

            .required {
                color: var(--color-error);
            }
        }
        
        .form-input,
        .form-textarea,
        .form-select {
            width: 100%;
            padding: 0.5rem 1rem;
            border: 1px solid var(--color-gray-300);
            border-radius: 8px;
            font-size: 1rem;
            font-family: var(--font-family-body), sans-serif;
            transition: border-color 0.2s ease, box-shadow 0.2s ease;
            
            &:focus {
                outline: none;
                border-color: var(--color-primary);
                box-shadow: 0 0 0 3px var(--color-primary-light);
            }
            
            &:disabled {
                background: var(--color-gray-50);
                color: var(--color-text-muted);
                cursor: not-allowed;
                opacity: 0.6;
            }
        }

        .form-select {
            background: var(--color-whitee);
            background-image: url("data:image/svg+xml,%3csvg xmlns='http://www.w3.org/2000/svg' fill='none' viewBox='0 0 20 20'%3e%3cpath stroke='%236b7280' stroke-linecap='round' stroke-linejoin='round' stroke-width='1.5' d='m6 8 4 4 4-4'/%3e%3c/svg%3e");
            background-position: right 0.5rem center;
            background-repeat: no-repeat;
            background-size: 1.5em 1.5em;
            padding-right: calc(#{1rem} + 1.5em);
            appearance: none;
            
            option {
                padding: 0.25rem;
                background: var(--color-whitee);
                color: var(--color-text-primary);

                &:checked {
                    background: var(--color-primary);
                    color: var(--color-whitee);
                }
                
                &:hover {
                    background: var(--color-primary-light);
                }
            }
        }
        
        .field-help {
            font-size: 0.875rem;
            color: var(--color-text-secondary);
            margin-top: 0.25rem;
        }
        
        .field-error {
            font-size: 0.875rem;
            color: var(--color-error);
            margin-top: 0.25rem;
        }
    }
    
    .checkbox-wrapper {
        display: flex;
        align-items: flex-start;
        gap: 1rem;

        input[type="checkbox"] {
            margin-top: 0.25rem;
        }
        
        .checkbox-label {
            flex: 1;
            margin: 0;
            
            .checkbox-description {
                display: block;
                font-weight: 400;
                color: var(--color-text-secondary);
                margin-top: 0.25rem;
            }
        }
    }
}

.loading-section,
.error-section {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 2rem;
    text-align: center;
    color: var(--color-text-secondary);
    background: var(--color-gray-50);
    border-radius: 8px;
}

.error-section {
    color: var(--color-error);
    background: var(--color-error-light);
}

.data-source-configuration {
    .stage-data-source-config {
        background: var(--color-whitee);
        border: 1px solid var(--color-gray-200);
        border-radius: 16px;
        padding: 1.5rem;
        margin-bottom: 1.5rem;

        .stage-header {
            display: flex;
            align-items: center;
            gap: 1rem;
            margin-bottom: 1rem;
            
            .stage-icon {
                font-size: 1.25rem;

                &.annotation {
                    color: var(--color-primary);
                }

                &.revision {
                    color: var(--color-warning);
                }

                &.completion {
                    color: var(--color-success);
                }
            }
            
            h4 {
                flex: 1;
                margin: 0;
                color: var(--color-text-primary);
                font-family: var(--font-family-heading), sans-serif;
            }
            
            .stage-badge {
                background: var(--color-gray-100);
                color: var(--color-gray-700);
                padding: 0.25rem 0.5rem;
                border-radius: 16px;
                font-size: 0.75rem;
                font-weight: 500;
                text-transform: uppercase;
                
                &.required {
                    background: var(--color-error-light);
                    color: var(--color-error-dark);
                }
                
                &.optional {
                    background: var(--color-warning-light);
                    color: var(--color-warning-dark);
                }
            }
        }

        .data-source-selectors {
            .form-group {
                margin-bottom: 1rem;
                
                label {
                    display: block;
                    font-weight: 500;
                    font-family: var(--font-family-body), sans-serif;
                    color: var(--color-gray-700);
                    margin-bottom: 0.5rem;

                    .required {
                        color: var(--color-error);
                    }
                }
                
                .form-select {
                    width: 100%;
                    padding: 0.5rem 1rem;
                    border: 1px solid var(--color-gray-300);
                    border-radius: 8px;
                    font-size: 1rem;
                    font-family: var(--font-family-body), sans-serif;
                    transition: border-color 0.2s ease, box-shadow 0.2s ease;
                    background: var(--color-whitee);
                    background-image: url("data:image/svg+xml,%3csvg xmlns='http://www.w3.org/2000/svg' fill='none' viewBox='0 0 20 20'%3e%3cpath stroke='%236b7280' stroke-linecap='round' stroke-linejoin='round' stroke-width='1.5' d='m6 8 4 4 4-4'/%3e%3c/svg%3e");
                    background-position: right 0.5rem center;
                    background-repeat: no-repeat;
                    background-size: 1.5em 1.5em;
                    padding-right: 2.5rem;
                    appearance: none;
                    
                    &:focus {
                        outline: none;
                        border-color: var(--color-primary);
                        box-shadow: 0 0 0 3px var(--color-primary-light);
                    }
                    
                    &:disabled {
                        background: var(--color-gray-50);
                        color: var(--color-text-muted);
                        cursor: not-allowed;
                        opacity: 0.6;
                    }
                    
                    option {
                        padding: 0.25rem;
                        background: var(--color-white);
                        color: var(--color-text-primary);

                        &:checked {
                            background: var(--color-primary);
                            color: var(--color-white);
                        }
                        
                        &:hover {
                            background: var(--color-primary-light);
                        }
                    }
                }
                
                .field-help {
                    font-size: 0.875rem;
                    color: var(--color-text-secondary);
                    margin-top: 0.25rem;
                }
                
                .field-error {
                    font-size: 0.875rem;
                    color: var(--color-error);
                    margin-top: 0.25rem;
                }
            }
        }
    }
}

.workflow-preview {
    background: var(--color-gray-50);
    border-radius: 16px;
    padding: 1.5rem;
    margin-bottom: 2rem;
    
    h4 {
        margin-top: 0;
        margin-bottom: 1rem;
        color: var(--color-text-primary);
        font-family: var(--font-family-heading), sans-serif;
    }
    
    .workflow-stages {
        display: flex;
        align-items: center;
        gap: 1rem;
        flex-wrap: wrap;
        
        .workflow-stage {
            text-align: center;
            
            .stage-node {
                display: flex;
                flex-direction: column;
                align-items: center;
                gap: 0.5rem;
                padding: 1rem;
                background: var(--color-white);
                border-radius: 8px;
                border: 1px-thick solid;
                min-width: 120px;

                &.annotation {
                    border-color: var(--color-primary-light);
                }

                &.revision {
                    border-color: var(--color-warning-light);
                }

                &.completion {
                    border-color: var(--color-success-light);
                }

                svg {
                    font-size: 1.25rem;
                }
                
                span {
                    font-weight: 500;
                    font-size: 0.875rem;
                }
            }
            
            .stage-members {
                margin-top: 0.5rem;
                font-size: 0.75rem;
                color: var(--color-text-secondary);
                max-width: 120px;
                word-wrap: break-word;
            }
        }
        
        .workflow-arrow {
            color: var(--color-gray-400);
            font-size: 1.5rem;
        }
    }
}

.assignment-sections {
    .stage-assignment {
        background: var(--color-white);
        border: 1px solid var(--color-gray-200);
        border-radius: 16px;
        padding: 1.5rem;
        margin-bottom: 1.5rem;

        .stage-header {
            display: flex;
            align-items: center;
            gap: 1rem;
            margin-bottom: 1rem;
            
            .stage-icon {
                width: 1.25rem;

                &.annotation {
                    color: var(--color-primary);
                }

                &.revision {
                    color: var(--color-warning);
                }

                &.completion {
                    color: var(--color-success);
                }
            }
            
            h4 {
                flex: 1;
                margin: 0;
                color: var(--color-text-primary);
                font-family: var(--font-family-heading), sans-serif;
            }
            
            .stage-badge {
                background: var(--color-gray-100);
                color: var(--color-gray-700);
                padding: 0.25rem 0.5rem;
                border-radius: 16px;
                font-size: 0.75rem;
                font-weight: 500;
                text-transform: uppercase;
                
                &.required {
                    background: var(--color-error-light);
                    color: var(--color-error-dark);
                }
                
                &.optional {
                    background: var(--color-warning-light);
                    color: var(--color-warning-dark);
                }
            }
        }
        
        .member-selector {
            label {
                display: block;
                font-weight: 500;
                font-family: var(--font-family-body), sans-serif;
                color: var(--color-gray-700);
                margin-bottom: 1rem;
                
                .required {
                    color: var(--color-error);
                }
            }
            
            .member-grid {
                display: grid;
                grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
                gap: 1rem;
                
                .member-card {
                    display: flex;
                    align-items: center;
                    gap: 1rem;
                    padding: 0.5rem;
                    border: 1px solid var(--color-gray-200);
                    border-radius: 8px;
                    cursor: pointer;
                    transition: all 0.2s ease;
                    box-shadow: 0 1px 2px rgba(var(--color-black), 0.05);
                    
                    &:hover {
                        border-color: var(--color-primary-hover);
                        background: var(--color-primary-light);
                        box-shadow: 0 1px 3px rgba(var(--color-black), 0.05);
                    }
                    
                    &.selected {
                        border-color: var(--color-primary);
                        background: var(--color-primary-light);
                        box-shadow: 0 1px 3px rgba(var(--color-black), 0.05);
                    }
                    
                    input[type="checkbox"] {
                        pointer-events: none;
                    }
                    
                    .member-info {
                        flex: 1;
                        
                        .member-name {
                            font-weight: 500;
                            color: var(--color-text-primary);
                        }
                        
                        .member-role {
                            font-size: 0.875rem;
                            color: var(--color-text-secondary);
                            text-transform: capitalize;
                        }
                    }
                }
            }
            
            .field-error {
                font-size: 0.875rem;
                color: var(--color-error);
                margin-top: 0.5rem;
            }
        }
    }
}
</style>
