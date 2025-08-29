<template>
    <div class="project-health-widget">
        <div v-if="!data" class="no-data">
            <font-awesome-icon :icon="faInfoCircle" />
            <span>No project health data available</span>
        </div>

        <div v-else class="health-content">
            <!-- Key Metrics -->
            <div class="metrics-grid">
                <div class="metric-card completion">
                    <div class="metric-value">
                        {{ data.overallCompletionPercentage.toFixed(1) }}%
                    </div>
                    <div class="metric-label">Completion</div>
                    <div class="metric-progress">
                        <div
                            class="progress-bar"
                            :style="{
                                width: `${data.overallCompletionPercentage}%`,
                            }"
                        ></div>
                    </div>
                </div>

                <div class="metric-card quality">
                    <div class="metric-value">
                        {{ data.qualityScore.toFixed(1) }}
                    </div>
                    <div class="metric-label">Quality Score</div>
                    <div
                        class="quality-indicator"
                        :class="getQualityClass(data.qualityScore)"
                    >
                        <font-awesome-icon :icon="getQualityIcon(data.qualityScore)" />
                    </div>
                </div>

                <div class="metric-card productivity">
                    <div class="metric-value">
                        {{ data.productivityScore.toFixed(1) }}
                    </div>
                    <div class="metric-label">Productivity</div>
                    <div
                        class="productivity-indicator"
                        :class="getProductivityClass(data.productivityScore)"
                    >
                        <font-awesome-icon
                            :icon="getProductivityIcon(data.productivityScore)"
                        />
                    </div>
                </div>
            </div>

            <!-- Team Activity -->
            <div class="team-section">
                <div class="section-header">
                    <h4>Team Activity</h4>
                    <span class="last-activity"
                        >Last activity:
                        {{ formatDate(data.lastActivityAt) }}</span
                    >
                </div>

                <div class="team-stats">
                    <div class="stat-item">
                        <span class="stat-value">{{ data.activeMembers }}</span>
                        <span class="stat-label">Active Members</span>
                    </div>
                    <div class="stat-separator">/</div>
                    <div class="stat-item">
                        <span class="stat-value">{{ data.totalMembers }}</span>
                        <span class="stat-label">Total Members</span>
                    </div>
                </div>

                <div class="activity-progress">
                    <div
                        class="activity-bar"
                        :style="{
                            width: `${
                                (data.activeMembers / data.totalMembers) * 100
                            }%`,
                        }"
                    ></div>
                </div>
            </div>

            <!-- Assets Overview -->
            <div class="assets-section">
                <div class="section-header">
                    <h4>Assets Progress</h4>
                </div>

                <div class="assets-chart" ref="assetsChartRef"></div>

                <div class="assets-stats">
                    <div class="asset-stat">
                        <span class="asset-count">{{
                            data.completedAssets
                        }}</span>
                        <span class="asset-label">Completed</span>
                    </div>
                    <div class="asset-stat">
                        <span class="asset-count">{{
                            data.totalAssets - data.completedAssets
                        }}</span>
                        <span class="asset-label">Remaining</span>
                    </div>
                    <div class="asset-stat">
                        <span class="asset-count">{{ data.totalAssets }}</span>
                        <span class="asset-label">Total</span>
                    </div>
                </div>
            </div>

            <!-- Bottlenecks -->
            <div v-if="data.bottlenecks?.length" class="bottlenecks-section">
                <div class="section-header">
                    <h4>Bottlenecks</h4>
                    <font-awesome-icon :icon="faExclamationTriangle" class="bottleneck-icon" />
                </div>

                <div class="bottlenecks-list">
                    <div
                        v-for="bottleneck in data.bottlenecks"
                        :key="bottleneck"
                        class="bottleneck-item"
                    >
                        <font-awesome-icon :icon="faExclamationTriangle" />
                        <span>{{ bottleneck }}</span>
                    </div>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed, ref, onMounted, onUnmounted, watch } from "vue";
import { FontAwesomeIcon } from "@fortawesome/vue-fontawesome";
import { 
    faInfoCircle, 
    faCheckCircle, 
    faThumbsUp, 
    faMinusCircle, 
    faTimesCircle, 
    faArrowUp, 
    faMinus, 
    faArrowDown, 
    faExclamationTriangle 
} from "@fortawesome/free-solid-svg-icons";
import * as d3 from "d3";
import type { ProjectHealthDto } from "@/services/project/dashboard/dashboard.types";

interface Props {
    data?: ProjectHealthDto;
    loading?: boolean;
    error?: string | null;
}

const props = defineProps<Props>();

// Refs
const assetsChartRef = ref<HTMLElement>();

// Computed properties
const completionRate = computed(() => {
    if (!props.data) return 0;
    return (props.data.completedAssets / props.data.totalAssets) * 100;
});

// Methods
const getQualityClass = (score: number): string => {
    if (score >= 8) return "excellent";
    if (score >= 6) return "good";
    if (score >= 4) return "fair";
    return "poor";
};

const getQualityIcon = (score: number) => {
    if (score >= 8) return faCheckCircle;
    if (score >= 6) return faThumbsUp;
    if (score >= 4) return faMinusCircle;
    return faTimesCircle;
};

const getProductivityClass = (score: number): string => {
    if (score >= 8) return "high";
    if (score >= 6) return "medium";
    if (score >= 4) return "low";
    return "very-low";
};

const getProductivityIcon = (score: number) => {
    if (score >= 8) return faArrowUp;
    if (score >= 6) return faArrowUp;
    if (score >= 4) return faMinus;
    return faArrowDown;
};

const formatDate = (date: Date): string => {
    const now = new Date();
    const diffMs = now.getTime() - new Date(date).getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMins / 60);
    const diffDays = Math.floor(diffHours / 24);

    if (diffMins < 1) return "Just now";
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays < 7) return `${diffDays}d ago`;

    return new Date(date).toLocaleDateString();
};

// D3 Chart Creation
const createAssetsChart = () => {
    if (!assetsChartRef.value || !props.data) return;

    // Clear previous chart
    d3.select(assetsChartRef.value).selectAll("*").remove();

    const container = assetsChartRef.value;
    const containerRect = container.getBoundingClientRect();
    const margin = { top: 10, right: 10, bottom: 10, left: 10 };
    const width = containerRect.width - margin.left - margin.right;
    const height = 80 - margin.top - margin.bottom;

    if (width <= 0 || height <= 0) return;

    const svg = d3
        .select(container)
        .append("svg")
        .attr("width", width + margin.left + margin.right)
        .attr("height", height + margin.top + margin.bottom);

    const g = svg
        .append("g")
        .attr("transform", `translate(${margin.left},${margin.top})`);

    // Data for donut chart
    const data = [
        {
            label: "Completed",
            value: props.data.completedAssets,
            color: "var(--color-green-500)",
        },
        {
            label: "Remaining",
            value: props.data.totalAssets - props.data.completedAssets,
            color: "var(--color-gray-300)",
        },
    ];

    const radius = Math.min(width, height) / 2;
    const innerRadius = radius * 0.6;

    const pie = d3
        .pie<any>()
        .value((d) => d.value)
        .sort(null);

    const arc = d3.arc<any>().innerRadius(innerRadius).outerRadius(radius);

    const centerX = width / 2;
    const centerY = height / 2;

    // Create donut chart
    const arcs = g
        .selectAll(".arc")
        .data(pie(data))
        .enter()
        .append("g")
        .attr("class", "arc")
        .attr("transform", `translate(${centerX},${centerY})`);

    arcs.append("path")
        .attr("d", arc)
        .style("fill", (d) => d.data.color)
        .style("stroke", "var(--color-white)")
        .style("stroke-width", 2);

    // Add center text
    g.append("text")
        .attr("x", centerX)
        .attr("y", centerY)
        .attr("text-anchor", "middle")
        .attr("dominant-baseline", "central")
        .style("font-size", "14px")
        .style("font-weight", "bold")
        .style("fill", "var(--color-gray-800)")
        .text(`${completionRate.value.toFixed(0)}%`);
};

// Watchers and lifecycle
watch(
    () => props.data,
    () => {
        if (props.data) {
            setTimeout(createAssetsChart, 100);
        }
    },
    { immediate: true }
);

onMounted(() => {
    const resizeObserver = new ResizeObserver(() => {
        createAssetsChart();
    });

    if (assetsChartRef.value) {
        resizeObserver.observe(assetsChartRef.value);
    }

    onUnmounted(() => {
        resizeObserver.disconnect();
    });
});
</script>

<style scoped>
.project-health-widget {
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

.health-content {
    display: flex;
    flex-direction: column;
    gap: 1rem;
    height: auto;
}

/* Metrics Grid */
.metrics-grid {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 0.75rem;
}

.metric-card {
    background: var(--color-gray-100);
    border-radius: 6px;
    padding: 0.75rem;
    text-align: center;
    position: relative;
}

.metric-value {
    font-size: 1.125rem;
    font-weight: bold;
    color: var(--color-gray-800);
    margin-bottom: 0.125rem;
}

.metric-label {
    font-size: 0.6875rem;
    color: var(--color-gray-600);
    margin-bottom: 0.5rem;
}

.metric-progress {
    height: 3px;
    background: var(--color-gray-300);
    border-radius: 2px;
    overflow: hidden;
}

.progress-bar {
    height: 100%;
    background: var(--color-primary);
    transition: width 0.3s ease;
}

.quality-indicator,
.productivity-indicator {
    position: absolute;
    top: 0.5rem;
    right: 0.5rem;
    font-size: 0.75rem;
}

.quality-indicator.excellent,
.productivity-indicator.high {
    color: var(--color-success);
}

.quality-indicator.good,
.productivity-indicator.medium {
    color: var(--color-warning);
}

.quality-indicator.fair,
.productivity-indicator.low {
    color: var(--color-warning);
}

.quality-indicator.poor,
.productivity-indicator.very-low {
    color: var(--color-error);
}

/* Team Section */
.team-section {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.section-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
}

.section-header h4 {
    margin: 0;
    font-size: 0.75rem;
    font-weight: 600;
    color: var(--color-gray-800);
}

.last-activity {
    font-size: 10px;
    color: var(--color-gray-600);
}

.team-stats {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 0.5rem;
}

.stat-item {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 0.125rem;
}

.stat-value {
    font-size: 1rem;
    font-weight: bold;
    color: var(--color-gray-800);
}

.stat-label {
    font-size: 9px;
    color: var(--color-gray-600);
}

.stat-separator {
    font-size: 14px;
    color: var(--color-gray-300);
}

.activity-progress {
    height: 4px;
    background: var(--color-gray-300);
    border-radius: 2px;
    overflow: hidden;
}

.activity-bar {
    height: 100%;
    background: var(--color-secondary);
    transition: width 0.3s ease;
}

/* Assets Section */
.assets-section {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    flex: 1;
}

.assets-chart {
    height: 80px;
    width: 100%;
}

.assets-stats {
    display: flex;
    justify-content: space-around;
    gap: 0.5rem;
}

.asset-stat {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 0.125rem;
}

.asset-count {
    font-size: 0.875rem;
    font-weight: bold;
    color: var(--color-gray-800);
}

.asset-label {
    font-size: 0.6rem;
    color: var(--color-gray-600);
}

/* Bottlenecks Section */
.bottlenecks-section {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.bottleneck-icon {
    color: var(--color-warning);
    font-size: 0.875rem;
}

.bottlenecks-list {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
}

.bottleneck-item {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-size: 0.6875rem;
    color: var(--color-warning);
    background: var(--color-yellow-100);
    padding: 0.25rem 0.5rem;
    border-radius: 4px;
}

/* Responsive adjustments */
@media (max-width: 768px) {
    .metrics-grid {
        grid-template-columns: 1fr;
        gap: 0.25rem;
    }

    .metric-card {
        padding: 0.375rem;
    }

    .metric-value {
        font-size: 1rem;
    }
}
</style>
