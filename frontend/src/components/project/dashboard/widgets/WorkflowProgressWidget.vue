<template>
    <div class="workflow-progress-widget">
        <div v-if="!data?.length" class="no-data">
            <i class="icon-info"></i>
            <span>No workflow data available</span>
        </div>

        <div v-else class="workflow-content">
            <!-- Workflow Selector -->
            <div v-if="data.length > 1" class="workflow-selector">
                <select v-model="selectedWorkflowId" class="workflow-select">
                    <option
                        v-for="workflow in data"
                        :key="workflow.workflowId"
                        :value="workflow.workflowId"
                    >
                        {{ workflow.workflowName }}
                    </option>
                </select>
            </div>

            <!-- Selected Workflow Progress -->
            <div v-if="selectedWorkflow" class="workflow-progress">
                <!-- Overall Progress -->
                <div class="overall-progress">
                    <div class="progress-header">
                        <h4>{{ selectedWorkflow.workflowName }}</h4>
                        <span class="completion-rate"
                            >{{
                                selectedWorkflow.completionPercentage.toFixed(
                                    1
                                )
                            }}%</span
                        >
                    </div>

                    <div class="progress-bar-container">
                        <div class="progress-track">
                            <div
                                class="progress-fill"
                                :style="{
                                    width: `${selectedWorkflow.completionPercentage}%`,
                                }"
                            ></div>
                        </div>
                    </div>

                    <div class="task-summary">
                        <div class="task-stat completed">
                            <span class="count">{{
                                selectedWorkflow.completedTasks
                            }}</span>
                            <span class="label">Completed</span>
                        </div>
                        <div class="task-stat in-progress">
                            <span class="count">{{
                                selectedWorkflow.inProgressTasks
                            }}</span>
                            <span class="label">In Progress</span>
                        </div>
                        <div class="task-stat not-started">
                            <span class="count">{{
                                selectedWorkflow.notStartedTasks
                            }}</span>
                            <span class="label">Not Started</span>
                        </div>
                        <div class="task-stat total">
                            <span class="count">{{
                                selectedWorkflow.totalTasks
                            }}</span>
                            <span class="label">Total</span>
                        </div>
                    </div>
                </div>

                <!-- Stage Progress Chart -->
                <div class="stages-section">
                    <div class="section-header">
                        <h5>Stages Progress</h5>
                    </div>
                    <div class="stages-chart" ref="stagesChartRef"></div>
                </div>

                <!-- Stage Details -->
                <div class="stages-details">
                    <div
                        v-for="stage in selectedWorkflow.stageProgress"
                        :key="stage.workflowStageId"
                        class="stage-item"
                    >
                        <div class="stage-info">
                            <div class="stage-name">{{ stage.stageName }}</div>
                            <div class="stage-stats">
                                <span class="stage-completion"
                                    >{{
                                        Math.round(
                                            (stage.completedTasks /
                                                stage.totalTasks) *
                                                100
                                        ) || 0
                                    }}%</span
                                >
                                <span class="stage-tasks"
                                    >{{ stage.completedTasks }}/{{
                                        stage.totalTasks
                                    }}</span
                                >
                            </div>
                        </div>
                        <div class="stage-progress-bar">
                            <div
                                class="stage-progress-fill"
                                :style="{
                                    width: `${
                                        (stage.completedTasks /
                                            stage.totalTasks) *
                                            100 || 0
                                    }%`,
                                }"
                            ></div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed, ref, onMounted, onUnmounted, watch } from "vue";
import * as d3 from "d3";
import type { WorkflowProgressDto } from "@/types/dashboard/dashboard";

interface Props {
    data?: WorkflowProgressDto[];
    loading?: boolean;
    error?: string | null;
}

const props = defineProps<Props>();

// Refs
const stagesChartRef = ref<HTMLElement>();
const selectedWorkflowId = ref<number | null>(null);

// Computed properties
const selectedWorkflow = computed(() => {
    if (!props.data?.length) return null;
    if (selectedWorkflowId.value === null) return props.data[0];
    return (
        props.data.find((w) => w.workflowId === selectedWorkflowId.value) ||
        props.data[0]
    );
});

// Initialize selection
watch(
    () => props.data,
    (newData) => {
        if (newData?.length && !selectedWorkflowId.value) {
            selectedWorkflowId.value = newData[0].workflowId;
        }
    },
    { immediate: true }
);

// D3 Chart Creation
const createStagesChart = () => {
    if (!stagesChartRef.value || !selectedWorkflow.value) return;

    // Clear previous chart
    d3.select(stagesChartRef.value).selectAll("*").remove();

    const container = stagesChartRef.value;
    const containerRect = container.getBoundingClientRect();
    const margin = { top: 20, right: 10, bottom: 30, left: 10 };
    const width = containerRect.width - margin.left - margin.right;
    const height = 120 - margin.top - margin.bottom;

    if (width <= 0 || height <= 0) return;

    const svg = d3
        .select(container)
        .append("svg")
        .attr("width", width + margin.left + margin.right)
        .attr("height", height + margin.top + margin.bottom);

    const g = svg
        .append("g")
        .attr("transform", `translate(${margin.left},${margin.top})`);

    // Prepare data
    const stages = selectedWorkflow.value.stageProgress.sort(
        (a, b) => a.stageOrder - b.stageOrder
    );

    if (stages.length === 0) return;

    // Scales
    const xScale = d3
        .scaleBand()
        .domain(stages.map((d) => d.stageName))
        .range([0, width])
        .padding(0.2);

    const yScale = d3
        .scaleLinear()
        .domain([0, d3.max(stages, (d) => d.totalTasks) || 1])
        .range([height, 0]);

    // Create bars
    const bars = g
        .selectAll(".stage-bar")
        .data(stages)
        .enter()
        .append("g")
        .attr("class", "stage-bar")
        .attr("transform", (d) => `translate(${xScale(d.stageName)},0)`);

    // Background bars (total tasks)
    bars.append("rect")
        .attr("class", "bar-total")
        .attr("x", 0)
        .attr("y", (d) => yScale(d.totalTasks))
        .attr("width", xScale.bandwidth())
        .attr("height", (d) => height - yScale(d.totalTasks))
        .style("fill", "var(--color-outline-variant)")
        .style("rx", 2);

    // Completed tasks bars
    bars.append("rect")
        .attr("class", "bar-completed")
        .attr("x", 0)
        .attr("y", (d) => yScale(d.completedTasks))
        .attr("width", xScale.bandwidth())
        .attr("height", (d) => height - yScale(d.completedTasks))
        .style("fill", "var(--color-success)")
        .style("rx", 2);

    // In progress tasks bars (stacked on completed)
    bars.append("rect")
        .attr("class", "bar-in-progress")
        .attr("x", 0)
        .attr("y", (d) => yScale(d.completedTasks + d.inProgressTasks))
        .attr("width", xScale.bandwidth())
        .attr("height", (d) =>
            Math.max(
                0,
                yScale(d.completedTasks) -
                    yScale(d.completedTasks + d.inProgressTasks)
            )
        )
        .style("fill", "var(--color-warning)")
        .style("rx", 2);

    // Add stage labels
    g.selectAll(".stage-label")
        .data(stages)
        .enter()
        .append("text")
        .attr("class", "stage-label")
        .attr("x", (d) => (xScale(d.stageName) || 0) + xScale.bandwidth() / 2)
        .attr("y", height + 15)
        .attr("text-anchor", "middle")
        .style("font-size", "10px")
        .style("fill", "var(--color-gray-600)")
        .text((d) =>
            d.stageName.length > 8
                ? d.stageName.substring(0, 8) + "..."
                : d.stageName
        );

    // Add value labels on bars
    bars.append("text")
        .attr("class", "bar-label")
        .attr("x", xScale.bandwidth() / 2)
        .attr("y", (d) => yScale(d.totalTasks) - 5)
        .attr("text-anchor", "middle")
        .style("font-size", "9px")
        .style("fill", "var(--color-gray-800)")
        .style("font-weight", "bold")
        .text((d) => d.totalTasks);
};

// Watchers and lifecycle
watch(
    () => selectedWorkflow.value,
    () => {
        if (selectedWorkflow.value) {
            setTimeout(createStagesChart, 100);
        }
    },
    { immediate: true }
);

onMounted(() => {
    const resizeObserver = new ResizeObserver(() => {
        createStagesChart();
    });

    if (stagesChartRef.value) {
        resizeObserver.observe(stagesChartRef.value);
    }

    onUnmounted(() => {
        resizeObserver.disconnect();
    });
});
</script>

<style scoped>
.workflow-progress-widget {
    height: auto;
    display: flex;
    flex-direction: column;
}

.no-data {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    height: auto;
    min-height: 200px;
    color: var(--color-gray-600);
    gap: 0.5rem;
}

.workflow-content {
    display: flex;
    flex-direction: column;
    gap: 1rem;
    height: auto;
}

/* Workflow Selector */
.workflow-selector {
    display: flex;
    justify-content: center;
}

.workflow-select {
    padding: 0.5rem 0.75rem;
    border: 1px solid var(--color-gray-300);
    border-radius: 6px;
    font-size: 0.75rem;
    background: var(--color-white);
    color: var(--color-gray-800);
    min-width: 150px;
}

.workflow-select:focus {
    outline: none;
    border-color: var(--color-primary);
    box-shadow: 0 0 0 2px rgba(var(--color-primary-rgb), 0.2);
}

/* Overall Progress */
.overall-progress {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
}

.progress-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
}

.progress-header h4 {
    margin: 0;
    font-size: 0.875rem;
    font-weight: 600;
    color: var(--color-gray-800);
}

.completion-rate {
    font-size: 1rem;
    font-weight: bold;
    color: var(--color-primary);
}

.progress-bar-container {
    margin: 0.5rem 0;
}

.progress-track {
    height: 8px;
    background: var(--color-outline-variant);
    border-radius: 4px;
    overflow: hidden;
}

.progress-fill {
    height: 100%;
    background: linear-gradient(
        90deg,
        var(--color-primary),
        var(--color-primary-light)
    );
    transition: width 0.3s ease;
}

.task-summary {
    display: grid;
    grid-template-columns: repeat(4, 1fr);
    gap: 0.75rem;
}

.task-stat {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 0.125rem;
    padding: 0.5rem;
    background: var(--color-gray-100);
    border-radius: 6px;
}

.task-stat .count {
    font-size: 0.875rem;
    font-weight: bold;
    color: var(--color-gray-800);
}

.task-stat .label {
    font-size: 0.5625rem;
    color: var(--color-gray-600);
    text-align: center;
}

.task-stat.completed .count {
    color: var(--color-success);
}

.task-stat.in-progress .count {
    color: var(--color-warning);
}

.task-stat.not-started .count {
    color: var(--color-gray-300);
}

.task-stat.total .count {
    color: var(--color-primary);
}

/* Stages Section */
.stages-section {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    flex: 1;
}

.section-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
}

.section-header h5 {
    margin: 0;
    font-size: 0.75rem;
    font-weight: 600;
    color: var(--color-gray-800);
}

.stages-chart {
    height: 120px;
    width: 100%;
    min-height: 120px;
}

/* Stage Details */
.stages-details {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.stage-item {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
    padding: 0.5rem;
    background: var(--color-gray-100);
    border-radius: 4px;
}

.stage-info {
    display: flex;
    justify-content: space-between;
    align-items: center;
}

.stage-name {
    font-size: 0.6875rem;
    font-weight: 500;
    color: var(--color-gray-800);
}

.stage-stats {
    display: flex;
    gap: 0.5rem;
    align-items: center;
}

.stage-completion {
    font-size: 0.625rem;
    font-weight: bold;
    color: var(--color-primary);
}

.stage-tasks {
    font-size: 9px;
    color: var(--color-gray-600);
}

.stage-progress-bar {
    height: 4px;
    background: var(--color-outline-variant);
    border-radius: 2px;
    overflow: hidden;
}

.stage-progress-fill {
    height: 100%;
    background: var(--color-primary);
    transition: width 0.3s ease;
}

/* Responsive adjustments */
@media (max-width: 768px) {
    .task-summary {
        grid-template-columns: repeat(2, 1fr);
    }

    .task-stat {
        padding: 0.25rem;
    }

    .task-stat .count {
        font-size: 0.75rem;
    }

    .task-stat .label {
        font-size: 0.5rem;
    }
}
</style>
