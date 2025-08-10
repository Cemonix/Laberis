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
                                :class="{ 
                                    'success': form.name.trim().length >= 3 && !errors.name && !existingWorkflows.includes(form.name.trim().toLowerCase()),
                                    'error': errors.name
                                }"
                            />
                            <div v-if="errors.name" class="field-error">
                                {{ errors.name }}
                                <span v-if="errors.name.includes('already exists')" class="suggestion-text">
                                    Try: "{{ getSuggestedName(form.name) }}"
                                </span>
                            </div>
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
                            <label for="label-scheme-select">Label Scheme <span class="required">*</span></label>
                            <div v-if="loadingLabelSchemes" class="loading-message">
                                <font-awesome-icon :icon="faSpinner" spin />
                                Loading label schemes...
                            </div>
                            <div v-else-if="labelSchemeError" class="error-message">
                                <font-awesome-icon :icon="faExclamationTriangle" />
                                {{ labelSchemeError }}
                            </div>
                            <select 
                                v-else-if="availableLabelSchemes.length > 0"
                                id="label-scheme-select"
                                v-model="form.labelSchemeId"
                                :disabled="isLoading"
                                class="form-select"
                                :class="{ 'error': errors.labelSchemeId }"
                                required
                            >
                                <option value="">Select a label scheme...</option>
                                <option 
                                    v-for="scheme in availableLabelSchemes" 
                                    :key="scheme.labelSchemeId" 
                                    :value="scheme.labelSchemeId"
                                >
                                    {{ scheme.name }}{{ scheme.isDefault ? ' (Default)' : '' }}
                                </option>
                            </select>
                            <div v-else class="no-label-schemes-message">
                                <font-awesome-icon :icon="faExclamationTriangle" />
                                <p>No label schemes available in this project.</p>
                                <small>You need to create at least one label scheme before creating workflows.</small>
                            </div>
                            <div v-if="errors.labelSchemeId" class="field-error">{{ errors.labelSchemeId }}</div>
                            <div v-if="availableLabelSchemes.length > 0" class="field-help">All annotations in this workflow will use this label scheme</div>
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
                                            :disabled="isDataSourceConflicted(dataSource.id)"
                                        >
                                            {{ dataSource.name }} ({{ dataSource.assetCount || 0 }} assets)
                                            <span v-if="isDataSourceConflicted(dataSource.id)"> - Already in use</span>
                                        </option>
                                    </select>
                                    <div v-if="errors.annotationInputDataSourceId" class="field-error">{{ errors.annotationInputDataSourceId }}</div>
                                    <div v-if="form.annotationInputDataSourceId && isDataSourceConflicted(form.annotationInputDataSourceId)" class="field-warning">
                                        <font-awesome-icon :icon="faExclamationTriangle" />
                                        This data source is already used by: {{ getDataSourceConflictInfo(form.annotationInputDataSourceId).join(', ') }}
                                    </div>
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
                                            :disabled="isDataSourceConflicted(dataSource.id)"
                                        >
                                            {{ dataSource.name }} ({{ dataSource.assetCount || 0 }} assets)
                                            <span v-if="isDataSourceConflicted(dataSource.id)"> - Already in use</span>
                                        </option>
                                    </select>
                                    <div v-if="errors.revisionInputDataSourceId" class="field-error">{{ errors.revisionInputDataSourceId }}</div>
                                    <div v-if="form.revisionInputDataSourceId && isDataSourceConflicted(form.revisionInputDataSourceId)" class="field-warning">
                                        <font-awesome-icon :icon="faExclamationTriangle" />
                                        This data source is already used by: {{ getDataSourceConflictInfo(form.revisionInputDataSourceId).join(', ') }}
                                    </div>
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
                                        <option value="">Select a data source for completion stage...</option>
                                        <option 
                                            v-for="dataSource in availableDataSourcesForCompletion" 
                                            :key="dataSource.id"
                                            :value="dataSource.id"
                                            :disabled="isDataSourceConflicted(dataSource.id)"
                                        >
                                            {{ dataSource.name }} ({{ dataSource.assetCount || 0 }} assets)
                                            <span v-if="isDataSourceConflicted(dataSource.id)"> - Already in use</span>
                                        </option>
                                    </select>
                                    <div v-if="form.completionInputDataSourceId && isDataSourceConflicted(form.completionInputDataSourceId)" class="field-warning">
                                        <font-awesome-icon :icon="faExclamationTriangle" />
                                        This data source is already used by: {{ getDataSourceConflictInfo(form.completionInputDataSourceId).join(', ') }}
                                    </div>
                                    <div class="field-help">Assets will automatically move to this data source when they reach the completion stage</div>
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
                                    <div class="field-help role-help">Only {{ getRoleDescriptionForStageType(WorkflowStageType.ANNOTATION) }} can be assigned to annotation stages</div>
                                    <div v-if="annotationMembers.length === 0" class="no-members-message">
                                        <font-awesome-icon :icon="faExclamationTriangle" />
                                        <span>No team members with {{ getRoleDescriptionForStageType(WorkflowStageType.ANNOTATION) }} roles are available for this stage.</span>
                                    </div>
                                    <div v-else class="member-grid">
                                        <div 
                                            v-for="member in annotationMembers" 
                                            :key="member.id"
                                            class="member-card"
                                            :class="{ 
                                                selected: form.annotationMembers.includes(member.id),
                                                disabled: isMemberAssignedToOtherStages(member.id, 'annotationMembers') && !form.annotationMembers.includes(member.id)
                                            }"
                                            @click="toggleMemberAssignment('annotationMembers', member.id)"
                                        >
                                            <input 
                                                type="checkbox" 
                                                :checked="form.annotationMembers.includes(member.id)"
                                                :disabled="isMemberAssignedToOtherStages(member.id, 'annotationMembers') && !form.annotationMembers.includes(member.id)"
                                                @click.stop
                                                @change="toggleMemberAssignment('annotationMembers', member.id)"
                                            />
                                            <div class="member-info">
                                                <div class="member-name">{{ member.userName || member.email }}</div>
                                                <div class="member-role">{{ member.role }}</div>
                                                <div v-if="isMemberAssignedToOtherStages(member.id, 'annotationMembers') && !form.annotationMembers.includes(member.id)" class="member-warning">
                                                    Already assigned
                                                </div>
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
                                    <div class="field-help role-help">Only {{ getRoleDescriptionForStageType(WorkflowStageType.REVISION) }} can be assigned to revision stages</div>
                                    <div v-if="revisionMembers.length === 0" class="no-members-message">
                                        <font-awesome-icon :icon="faExclamationTriangle" />
                                        <span>No team members with {{ getRoleDescriptionForStageType(WorkflowStageType.REVISION) }} roles are available for this stage.</span>
                                    </div>
                                    <div v-else class="member-grid">
                                        <div 
                                            v-for="member in revisionMembers" 
                                            :key="member.id"
                                            class="member-card"
                                            :class="{ 
                                                selected: form.revisionMembers.includes(member.id),
                                                disabled: isMemberAssignedToOtherStages(member.id, 'revisionMembers') && !form.revisionMembers.includes(member.id)
                                            }"
                                            @click="toggleMemberAssignment('revisionMembers', member.id)"
                                        >
                                            <input 
                                                type="checkbox" 
                                                :checked="form.revisionMembers.includes(member.id)"
                                                :disabled="isMemberAssignedToOtherStages(member.id, 'revisionMembers') && !form.revisionMembers.includes(member.id)"
                                                @click.stop
                                                @change="toggleMemberAssignment('revisionMembers', member.id)"
                                            />
                                            <div class="member-info">
                                                <div class="member-name">{{ member.userName || member.email }}</div>
                                                <div class="member-role">{{ member.role }}</div>
                                                <div v-if="isMemberAssignedToOtherStages(member.id, 'revisionMembers') && !form.revisionMembers.includes(member.id)" class="member-warning">
                                                    Already assigned
                                                </div>
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
                                    <div class="field-help role-help">Only {{ getRoleDescriptionForStageType(WorkflowStageType.COMPLETION) }} can be assigned to completion stages</div>
                                    <div v-if="completionMembers.length === 0" class="no-members-message">
                                        <font-awesome-icon :icon="faExclamationTriangle" />
                                        <span>No team members with {{ getRoleDescriptionForStageType(WorkflowStageType.COMPLETION) }} roles are available for this stage.</span>
                                    </div>
                                    <div v-else class="member-grid">
                                        <div 
                                            v-for="member in completionMembers" 
                                            :key="member.id"
                                            class="member-card"
                                            :class="{ 
                                                selected: form.completionMembers.includes(member.id),
                                                disabled: isMemberAssignedToOtherStages(member.id, 'completionMembers') && !form.completionMembers.includes(member.id)
                                            }"
                                            @click="toggleMemberAssignment('completionMembers', member.id)"
                                        >
                                            <input 
                                                type="checkbox" 
                                                :checked="form.completionMembers.includes(member.id)"
                                                :disabled="isMemberAssignedToOtherStages(member.id, 'completionMembers') && !form.completionMembers.includes(member.id)"
                                                @click.stop
                                                @change="toggleMemberAssignment('completionMembers', member.id)"
                                            />
                                            <div class="member-info">
                                                <div class="member-name">{{ member.userName || member.email }}</div>
                                                <div class="member-role">{{ member.role }}</div>
                                                <div v-if="isMemberAssignedToOtherStages(member.id, 'completionMembers') && !form.completionMembers.includes(member.id)" class="member-warning">
                                                    Already assigned
                                                </div>
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
import type {CreateWorkflowWithStagesRequest, Workflow} from '@/types/workflow';
import {WorkflowStageType} from '@/types/workflow';
import type {ProjectMember} from '@/types/projectMember';
import type {DataSource} from '@/types/dataSource';
import type {LabelScheme} from '@/types/label/labelScheme';
import {projectMemberService, dataSourceService, labelSchemeService, workflowService} from '@/services/api/projects';
import {AppLogger} from '@/utils/logger';
import {useToast} from '@/composables/useToast';
import {filterMembersByStageType, getRoleDescriptionForStageType} from '@/core/validation/workflowRoleValidation';

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
        validation: () => form.name.trim().length > 0 && form.labelSchemeId !== ''
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
const dataSourceConflicts = ref<Map<number, string[]>>(new Map());

const projectMembers = ref<ProjectMember[]>([]);
const availableDataSources = ref<DataSource[]>([]);
const availableLabelSchemes = ref<LabelScheme[]>([]);
const loadingLabelSchemes = ref(false);
const labelSchemeError = ref<string>('');
const existingWorkflows = ref<string[]>([]);
const workflowLookup = ref<Map<number, string>>(new Map());

// Form data
interface WorkflowWizardForm {
    name: string;
    description?: string;
    includeRevision: boolean;
    labelSchemeId: number | '';
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
    labelSchemeId: '',
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
    labelSchemeId: '',
    annotationInputDataSourceId: '',
    revisionInputDataSourceId: '',
    annotationMembers: '',
    completionMembers: ''
});

// Helper functions for data source availability
const isDataSourceConflicted = (dataSourceId: number): boolean => {
    return dataSourceConflicts.value.has(dataSourceId);
};

const getDataSourceConflictInfo = (dataSourceId: number): string[] => {
    return dataSourceConflicts.value.get(dataSourceId) || [];
};

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
        // Fetch all data sources by getting the first page and checking total count
        let allDataSources: DataSource[] = [];
        let currentPage = 1;
        const pageSize = 50;
        let hasMore = true;
        
        while (hasMore) {
            const response = await dataSourceService.getDataSources({ 
                projectId: props.projectId,
                pageNumber: currentPage,
                pageSize: pageSize
            });
            
            allDataSources = [...allDataSources, ...response.data];
            
            // Check if we need to fetch more pages
            hasMore = response.currentPage < response.totalPages;
            currentPage++;
            
            // Safety check to prevent infinite loop
            if (currentPage > 10) {
                logger.warn('Too many pages of data sources, stopping at page 10');
                break;
            }
        }
        
        availableDataSources.value = allDataSources;
        logger.info(`Loaded ${availableDataSources.value.length} data sources from ${currentPage - 1} page(s)`);
        
        // Check for conflicts for each data source
        await checkDataSourceConflicts();
    } catch (error) {
        dataSourceError.value = 'Failed to load data sources';
        showApiError(error, 'Failed to load data sources');
    } finally {
        loadingDataSources.value = false;
    }
};

const checkDataSourceConflicts = async () => {
    const conflicts = new Map<number, string[]>();
    
    for (const dataSource of availableDataSources.value) {
        try {
            const conflictingStages = await dataSourceService.getDataSourceUsageConflicts(
                props.projectId, 
                dataSource.id
            );
            
            if (conflictingStages.length > 0) {
                const conflictDescriptions = conflictingStages.map(stage => {
                    const workflowName = workflowLookup.value.get(stage.workflowId) || `ID: ${stage.workflowId}`;
                    return `${stage.name} (Workflow: ${workflowName})`;
                });
                conflicts.set(dataSource.id, conflictDescriptions);
            }
        } catch (error) {
            logger.warn(`Failed to check conflicts for data source ${dataSource.id}:`, error);
            // Continue checking other data sources even if one fails
        }
    }
    
    dataSourceConflicts.value = conflicts;
    logger.info(`Found conflicts for ${conflicts.size} data sources`);
};

const fetchLabelSchemes = async () => {
    loadingLabelSchemes.value = true;
    labelSchemeError.value = '';
    try {
        const response = await labelSchemeService.getLabelSchemesForProject(props.projectId);
        availableLabelSchemes.value = response.data;
        logger.info(`Loaded ${availableLabelSchemes.value.length} label schemes`);
    } catch (error) {
        labelSchemeError.value = 'Failed to load label schemes';
        showApiError(error, 'Failed to load label schemes');
    } finally {
        loadingLabelSchemes.value = false;
    }
};

const fetchExistingWorkflows = async () => {
    try {
        // Fetch all workflows by getting the first page and checking total count
        let allWorkflows: Workflow[] = [];
        let currentPage = 1;
        const pageSize = 50;
        let hasMore = true;
        
        while (hasMore) {
            const response = await workflowService.getWorkflows(props.projectId, { 
                pageNumber: currentPage,
                pageSize: pageSize 
            });
            
            allWorkflows = [...allWorkflows, ...response.data];
            
            // Check if we need to fetch more pages
            hasMore = response.currentPage < response.totalPages;
            currentPage++;
            
            // Safety check to prevent infinite loop
            if (currentPage > 10) {
                logger.warn('Too many pages of workflows, stopping at page 10');
                break;
            }
        }
        
        // Populate existing workflow names for duplicate checking
        existingWorkflows.value = allWorkflows.map(workflow => workflow.name.toLowerCase());
        
        // Populate workflow lookup for conflict display
        const lookup = new Map<number, string>();
        allWorkflows.forEach(workflow => {
            lookup.set(workflow.id, workflow.name);
        });
        workflowLookup.value = lookup;
        
        logger.info(`Loaded ${existingWorkflows.value.length} existing workflows from ${currentPage - 1} page(s)`);
    } catch (error) {
        logger.error('Failed to fetch existing workflows:', error);
        // Don't show error to user as this is not critical for workflow creation
    }
};

// Navigation logic
const canProceedToNextStep = computed(() => {
    if (isLoading.value) return false;
    
    // Run validation and check if there are no errors
    if (!isCurrentStepValid()) {
        return false;
    }
    return true;
});

// Filtered members by stage type
const annotationMembers = computed(() => 
    filterMembersByStageType(projectMembers.value, WorkflowStageType.ANNOTATION)
);

const revisionMembers = computed(() => 
    filterMembersByStageType(projectMembers.value, WorkflowStageType.REVISION)
);

const completionMembers = computed(() => 
    filterMembersByStageType(projectMembers.value, WorkflowStageType.COMPLETION)
);

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

const getCurrentStepName = () => {
    return stepperSteps[0].id;
};

const isCurrentStepValid = () => {
    switch (getCurrentStepName()) {
        case 'basic':
            return validateFirstStep();
        case 'datasources':
            return validateSecondStep();
        case 'members':
            return validateThirdStep();
        default:
            return true;
    }
};

const validateFirstStep = () => {
    // Basic validation for the first step
    errors.name = ''; // Clear previous errors
    errors.labelSchemeId = '';
    
    let isValid = true;
    
    if (!form.name.trim()) {
        errors.name = 'Workflow name is required';
        isValid = false;
    } else if (form.name.trim().length < 3) {
        errors.name = 'Workflow name must be at least 3 characters long';
        isValid = false;
    } else if (existingWorkflows.value.includes(form.name.trim().toLowerCase())) {
        errors.name = 'A workflow with this name already exists in this project';
        isValid = false;
    }
    
    if (!form.labelSchemeId) {
        if (availableLabelSchemes.value.length === 0) {
            errors.labelSchemeId = 'No label schemes available. Create a label scheme first.';
        } else {
            errors.labelSchemeId = 'Please select a label scheme for this workflow';
        }
        isValid = false;
    }
    
    return isValid;
};

const validateSecondStep = () => {
    // Data source validation for the second step
    errors.annotationInputDataSourceId = ''; // Clear previous errors
    errors.revisionInputDataSourceId = '';
    
    if (!form.annotationInputDataSourceId) {
        errors.annotationInputDataSourceId = 'Annotation stage must have exactly one input data source';
        return false;
    }
    
    // Check if selected annotation data source is conflicted
    if (form.annotationInputDataSourceId && isDataSourceConflicted(form.annotationInputDataSourceId)) {
        errors.annotationInputDataSourceId = 'This data source is already in use by another workflow. Please select a different data source.';
        return false;
    }
    
    if (form.includeRevision && !form.revisionInputDataSourceId) {
        errors.revisionInputDataSourceId = 'Revision stage must have exactly one input data source';
        return false;
    }
    
    // Check if selected revision data source is conflicted
    if (form.revisionInputDataSourceId && isDataSourceConflicted(form.revisionInputDataSourceId)) {
        errors.revisionInputDataSourceId = 'This data source is already in use by another workflow. Please select a different data source.';
        return false;
    }
    
    return true;
};

const validateThirdStep = () => {
    // Member assignment validation for the third step
    errors.annotationMembers = ''; // Clear previous errors
    errors.completionMembers = '';
    
    // Check if members are available for each stage
    if (annotationMembers.value.length === 0) {
        errors.annotationMembers = `No team members with ${getRoleDescriptionForStageType(WorkflowStageType.ANNOTATION)} roles are available. Add members with appropriate roles to continue.`;
        return false;
    }
    if (completionMembers.value.length === 0) {
        errors.completionMembers = `No team members with ${getRoleDescriptionForStageType(WorkflowStageType.COMPLETION)} roles are available. Add members with appropriate roles to continue.`;
        return false;
    }
    
    // Check if at least one member is assigned to required stages
    if (form.annotationMembers.length === 0) {
        errors.annotationMembers = 'At least one member must be assigned to annotation stage';
        return false;
    }
    if (form.completionMembers.length === 0) {
        errors.completionMembers = 'At least one member must be assigned to completion stage';
        return false;
    }
    return true;
};

// Member assignment functions are defined after the watchers

// Form submission
const handleSubmit = async () => {
    if (!isCurrentStepValid()) {
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
            labelSchemeId: form.labelSchemeId as number,
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
    fetchLabelSchemes();
    fetchExistingWorkflows();
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

// Real-time validation for workflow name
watch(() => form.name, (newName) => {
    if (newName.trim()) {
        // Clear any existing name error first
        errors.name = '';
        
        if (newName.trim().length < 3) {
            errors.name = 'Workflow name must be at least 3 characters long';
        } else if (existingWorkflows.value.includes(newName.trim().toLowerCase())) {
            errors.name = 'A workflow with this name already exists in this project';
        }
    } else {
        errors.name = '';
    }
});

// Clear conflicted data sources when conflicts change
watch(() => dataSourceConflicts.value, () => {
    // Clear annotation data source if it's now conflicted
    if (form.annotationInputDataSourceId && isDataSourceConflicted(form.annotationInputDataSourceId)) {
        form.annotationInputDataSourceId = '';
        logger.info('Cleared conflicted annotation data source');
    }
    
    // Clear revision data source if it's now conflicted
    if (form.revisionInputDataSourceId && isDataSourceConflicted(form.revisionInputDataSourceId)) {
        form.revisionInputDataSourceId = '';
        logger.info('Cleared conflicted revision data source');
    }
    
    // Clear completion data source if it's now conflicted
    if (form.completionInputDataSourceId && isDataSourceConflicted(form.completionInputDataSourceId)) {
        form.completionInputDataSourceId = '';
        logger.info('Cleared conflicted completion data source');
    }
}, { deep: true });

// Member assignment functions
const toggleMemberAssignment = (stageKey: keyof Pick<WorkflowWizardForm, 'annotationMembers' | 'revisionMembers' | 'completionMembers'>, memberId: number) => {
    const members = form[stageKey] as number[];
    const memberIndex = members.indexOf(memberId);
    
    if (memberIndex > -1) {
        // Member is already assigned, remove them
        members.splice(memberIndex, 1);
    } else {
        // Member is not assigned, check if they're assigned to other stages
        const memberAlreadyAssigned = isMemberAssignedToOtherStages(memberId, stageKey);
        
        if (memberAlreadyAssigned) {
            // Show a warning or prevent assignment
            logger.warn(`Member ${memberId} is already assigned to another stage`);
            return; // Prevent assignment to multiple stages
        }
        
        // Add member to this stage
        members.push(memberId);
    }
};

const isMemberAssignedToOtherStages = (memberId: number, currentStage: keyof Pick<WorkflowWizardForm, 'annotationMembers' | 'revisionMembers' | 'completionMembers'>): boolean => {
    const stages = ['annotationMembers', 'revisionMembers', 'completionMembers'] as const;
    
    return stages.some(stage => {
        if (stage === currentStage) return false;
        return form[stage].includes(memberId);
    });
};

const getSelectedMemberNames = (memberIds: number[]): string[] => {
    return memberIds
        .map(id => projectMembers.value.find(member => member.id === id))
        .filter(Boolean)
        .map(member => member!.userName || member!.email);
};

const getSuggestedName = (baseName: string): string => {
    if (!baseName.trim()) return '';
    
    let counter = 1;
    let suggestedName = `${baseName.trim()} ${counter}`;
    
    while (existingWorkflows.value.includes(suggestedName.toLowerCase())) {
        counter++;
        suggestedName = `${baseName.trim()} ${counter}`;
    }
    
    return suggestedName;
};
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
            
            &.success {
                border-color: #10b981;
                box-shadow: 0 0 0 3px rgba(16, 185, 129, 0.1);
            }
            
            &.error {
                border-color: var(--color-error);
                box-shadow: 0 0 0 3px rgba(239, 68, 68, 0.1);
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
                
                &:disabled {
                    color: var(--color-text-muted);
                    background: var(--color-gray-100);
                    cursor: not-allowed;
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
                        
                        &:disabled {
                            color: var(--color-text-muted);
                            background: var(--color-gray-100);
                            cursor: not-allowed;
                        }
                    }
                }
                
                .field-help {
                    font-size: 0.875rem;
                    color: var(--color-text-secondary);
                    margin-top: 0.25rem;
                    
                    &.role-help {
                        color: var(--color-primary);
                        font-weight: 500;
                        background: var(--color-primary-light);
                        padding: 0.5rem;
                        border-radius: 4px;
                        margin-bottom: 1rem;
                        border-left: 3px solid var(--color-primary);
                    }
                }
                
                .field-error {
                    font-size: 0.875rem;
                    color: var(--color-error);
                    margin-top: 0.25rem;
                }
                
                .field-warning {
                    font-size: 0.875rem;
                    color: var(--color-warning);
                    margin-top: 0.25rem;
                    padding: 0.5rem;
                    background: var(--color-warning-light);
                    border: 1px solid var(--color-warning);
                    border-radius: 4px;
                    display: flex;
                    align-items: center;
                    gap: 0.5rem;
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
                    
                    &.disabled {
                        opacity: 0.6;
                        cursor: not-allowed;
                        background: var(--color-gray-50);
                        border-color: var(--color-gray-200);
                        
                        &:hover {
                            border-color: var(--color-gray-200);
                            background: var(--color-gray-50);
                            box-shadow: none;
                        }
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
                        
                        .member-warning {
                            font-size: 0.75rem;
                            color: var(--color-warning);
                            font-weight: 500;
                            margin-top: 0.25rem;
                        }
                    }
                }
            }
            
            .field-error {
                font-size: 0.875rem;
                color: var(--color-error);
                margin-top: 0.5rem;
                
                .suggestion-text {
                    display: block;
                    color: var(--color-text-secondary);
                    font-style: italic;
                    font-weight: normal;
                    margin-top: 0.25rem;
                    
                    &::before {
                        content: "💡 ";
                    }
                }
            }
            
            .no-members-message {
                display: flex;
                align-items: center;
                gap: 0.5rem;
                padding: 1rem;
                background: var(--color-warning-light);
                border: 1px solid var(--color-warning);
                border-radius: 8px;
                color: var(--color-warning-dark);
                font-size: 0.875rem;
                
                svg {
                    color: var(--color-warning);
                }
            }
        }
    }
}
</style>
