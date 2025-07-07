<template>
    <div class="stepper">
        <!-- Stepper Header -->
        <div class="stepper-header">
            <div class="stepper-steps">
                <div 
                    v-for="(step, index) in steps" 
                    :key="step.id"
                    class="step"
                    :class="{ 
                        'active': currentStep === index, 
                        'completed': index < currentStep,
                        'disabled': index > currentStep
                    }"
                    @click="handleStepClick(index)"
                >
                    <div class="step-indicator">
                        <font-awesome-icon 
                            v-if="index < currentStep" 
                            :icon="faCheck" 
                            class="step-check"
                        />
                        <span v-else>{{ index + 1 }}</span>
                    </div>
                    <div class="step-content">
                        <div class="step-title">{{ step.title }}</div>
                        <div v-if="step.description" class="step-description">{{ step.description }}</div>
                    </div>
                </div>
            </div>
        </div>

        <!-- Step Content -->
        <div class="stepper-content">
            <slot 
                :current-step="currentStep" 
                :steps="steps"
                :can-proceed="canProceed"
                :next-step="nextStep"
                :previous-step="previousStep"
                :go-to-step="goToStep"
            />
        </div>

        <!-- Navigation (optional, can be overridden via slot) -->
        <div v-if="!hideNavigation" class="stepper-navigation">
            <slot 
                name="navigation"
                :current-step="currentStep"
                :steps="steps"
                :can-proceed="canProceed"
                :next-step="nextStep"
                :previous-step="previousStep"
                :is-first-step="currentStep === 0"
                :is-last-step="currentStep === steps.length - 1"
            >
                <!-- Default Navigation -->
                <div class="nav-left">
                    <Button
                        v-if="currentStep > 0"
                        variant="secondary"
                        @click="previousStep"
                        :disabled="loading"
                    >
                        <font-awesome-icon :icon="faArrowLeft" />
                        {{ previousButtonText }}
                    </Button>
                </div>

                <div class="nav-right">
                    <Button
                        v-if="showCancelButton"
                        variant="secondary"
                        @click="$emit('cancel')"
                        :disabled="loading"
                    >
                        {{ cancelButtonText }}
                    </Button>

                    <Button
                        v-if="currentStep < steps.length - 1"
                        variant="primary"
                        @click="nextStep"
                        :disabled="!canProceed || loading"
                    >
                        {{ nextButtonText }}
                        <font-awesome-icon :icon="faArrowRight" />
                    </Button>

                    <Button
                        v-else
                        variant="primary"
                        @click="$emit('submit')"
                        :loading="loading"
                        :disabled="!canProceed"
                    >
                        <font-awesome-icon v-if="submitIcon" :icon="submitIcon" />
                        {{ submitButtonText }}
                    </Button>
                </div>
            </slot>
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';
import { faCheck, faArrowLeft, faArrowRight } from '@fortawesome/free-solid-svg-icons';
import Button from '@/components/common/Button.vue';

export interface StepperStep {
    id: string;
    title: string;
    description?: string;
    validation?: () => boolean | Promise<boolean>;
}

interface Props {
    steps: StepperStep[];
    initialStep?: number;
    canProceed?: boolean;
    loading?: boolean;
    allowStepClick?: boolean;
    hideNavigation?: boolean;
    showCancelButton?: boolean;
    nextButtonText?: string;
    previousButtonText?: string;
    cancelButtonText?: string;
    submitButtonText?: string;
    submitIcon?: any;
}

const props = withDefaults(defineProps<Props>(), {
    initialStep: 0,
    canProceed: true,
    loading: false,
    allowStepClick: false,
    hideNavigation: false,
    showCancelButton: true,
    nextButtonText: 'Next',
    previousButtonText: 'Previous',
    cancelButtonText: 'Cancel',
    submitButtonText: 'Submit'
});

const emit = defineEmits<{
    'step-change': [currentStep: number, previousStep: number];
    'step-validation': [stepIndex: number, isValid: boolean];
    'next': [];
    'previous': [];
    'submit': [];
    'cancel': [];
}>();

const currentStep = ref(props.initialStep);

const nextStep = async () => {
    if (currentStep.value < props.steps.length - 1 && props.canProceed && !props.loading) {
        const currentStepConfig = props.steps[currentStep.value];
        
        // Validate current step if validation function is provided
        if (currentStepConfig.validation) {
            const isValid = await currentStepConfig.validation();
            emit('step-validation', currentStep.value, isValid);
            if (!isValid) return;
        }
        
        const previousStep = currentStep.value;
        currentStep.value++;
        emit('step-change', currentStep.value, previousStep);
        emit('next');
    }
};

const previousStep = () => {
    if (currentStep.value > 0 && !props.loading) {
        const previousStepIndex = currentStep.value;
        currentStep.value--;
        emit('step-change', currentStep.value, previousStepIndex);
        emit('previous');
    }
};

const goToStep = async (stepIndex: number) => {
    if (stepIndex >= 0 && stepIndex < props.steps.length && stepIndex !== currentStep.value) {
        // If moving forward, validate all steps in between
        if (stepIndex > currentStep.value) {
            for (let i = currentStep.value; i < stepIndex; i++) {
                const stepConfig = props.steps[i];
                if (stepConfig.validation) {
                    const isValid = await stepConfig.validation();
                    emit('step-validation', i, isValid);
                    if (!isValid) return;
                }
            }
        }
        
        const previousStepIndex = currentStep.value;
        currentStep.value = stepIndex;
        emit('step-change', currentStep.value, previousStepIndex);
    }
};

const handleStepClick = (stepIndex: number) => {
    if (props.allowStepClick) {
        goToStep(stepIndex);
    }
};

// Watch for external step changes
watch(() => props.initialStep, (newStep) => {
    currentStep.value = newStep;
});

// Expose methods and data for parent component
defineExpose({
    currentStep: computed(() => currentStep.value),
    nextStep,
    previousStep,
    goToStep,
    isFirstStep: computed(() => currentStep.value === 0),
    isLastStep: computed(() => currentStep.value === props.steps.length - 1)
});
</script>

<style scoped lang="scss">
@use '../../styles/variables' as *;

.stepper {
    .stepper-header {
        margin-bottom: $margin-xlarge;
        
        .stepper-steps {
            display: flex;
            justify-content: space-between;
            position: relative;
            
            &::before {
                content: '';
                position: absolute;
                top: $margin-large;
                left: $margin-large;
                right: $margin-large;
                height: $border-width-thick;
                background: $color-gray-200;
                z-index: 1;
            }
            
            .step {
                display: flex;
                flex-direction: column;
                align-items: center;
                flex: 1;
                position: relative;
                z-index: 2;
                cursor: default;
                
                &.completed .step-indicator {
                    background: $color-primary;
                    color: $color-white;
                    border-color: $color-primary;
                }
                
                &.active .step-indicator {
                    background: $color-primary-light;
                    color: $color-primary-dark;
                    border-color: $color-primary;
                    box-shadow: 0 0 0 3px $color-primary-light;
                }
                
                &.disabled {
                    opacity: 0.5;
                }
                
                .step-indicator {
                    width: 48px;
                    height: 48px;
                    border-radius: $border-radius-circle;
                    background: $color-white;
                    border: $border-width-thick solid $color-gray-300;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    font-weight: $font-weight-large;
                    margin-bottom: $margin-small;
                    transition: all 0.2s ease;
                    
                    .step-check {
                        font-size: $font-size-large;
                    }
                }
                
                .step-content {
                    text-align: center;
                    
                    .step-title {
                        font-weight: $font-weight-large;
                        color: $color-text-primary;
                        margin-bottom: $margin-xsmall;
                        font-family: $font-family-heading;
                    }
                    
                    .step-description {
                        font-size: $font-size-small;
                        color: $color-text-secondary;
                    }
                }
            }
        }
    }
    
    .stepper-content {
        min-height: 400px;
        max-height: 50vh;
        overflow-y: auto;
        padding-right: $padding-small;
        
        /* Custom scrollbar styling */
        &::-webkit-scrollbar {
            width: 8px;
        }
        
        &::-webkit-scrollbar-track {
            background: $color-gray-100;
            border-radius: $border-radius-lg;
        }
        
        &::-webkit-scrollbar-thumb {
            background: $color-gray-300;
            border-radius: $border-radius-lg;
            
            &:hover {
                background: $color-gray-400;
            }
        }
        
        /* Firefox scrollbar styling */
        scrollbar-width: thin;
        scrollbar-color: $color-gray-300 $color-gray-100;
        
        /* Responsive height adjustments */
        @media (max-height: 800px) {
            max-height: 60vh;
        }
        
        @media (max-height: 600px) {
            max-height: 50vh;
        }
    }
    
    .stepper-navigation {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-top: $margin-xlarge;
        padding-top: $padding-xlarge;
        border-top: $border-width solid $color-border-light;
        
        .nav-left,
        .nav-right {
            display: flex;
            gap: $gap-medium;
        }
    }
}
</style>
