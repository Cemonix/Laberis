<template>
    <div class="user-performance-widget">
        <div v-if="!data?.length" class="no-data">
            <i class="icon-info"></i>
            <span>No user performance data available</span>
        </div>

        <div v-else class="performance-content">
            <!-- User Selector -->
            <div v-if="data.length > 1" class="user-selector">
                <select v-model="selectedUserId" class="user-select">
                    <option value="">All Users</option>
                    <option
                        v-for="user in data"
                        :key="user.userId"
                        :value="user.userId"
                    >
                        {{ user.userName }}
                    </option>
                </select>
            </div>

            <!-- Performance Overview -->
            <div v-if="selectedUser" class="user-overview">
                <div class="user-info">
                    <div class="user-header">
                        <h4>{{ selectedUser.userName }}</h4>
                        <span class="user-role">{{ selectedUser.role }}</span>
                    </div>
                    <div class="user-stats">
                        <span class="last-activity"
                            >Last active:
                            {{ formatDate(selectedUser.lastActivityAt) }}</span
                        >
                    </div>
                </div>

                <div class="performance-metrics">
                    <div class="metric-card completion-rate">
                        <div class="metric-value">
                            {{ selectedUser.completionRate.toFixed(1) }}%
                        </div>
                        <div class="metric-label">Completion Rate</div>
                        <div
                            class="metric-indicator"
                            :class="
                                getCompletionClass(selectedUser.completionRate)
                            "
                        >
                            <i
                                :class="
                                    getCompletionIcon(
                                        selectedUser.completionRate
                                    )
                                "
                            ></i>
                        </div>
                    </div>

                    <div class="metric-card veto-rate">
                        <div class="metric-value">
                            {{ selectedUser.vetoRate.toFixed(1) }}%
                        </div>
                        <div class="metric-label">Veto Rate</div>
                        <div
                            class="metric-indicator"
                            :class="getVetoClass(selectedUser.vetoRate)"
                        >
                            <i :class="getVetoIcon(selectedUser.vetoRate)"></i>
                        </div>
                    </div>

                    <div class="metric-card productivity">
                        <div class="metric-value">
                            {{ selectedUser.tasksPerDay.toFixed(1) }}
                        </div>
                        <div class="metric-label">Tasks/Day</div>
                        <div class="metric-icon">
                            <i class="icon-zap"></i>
                        </div>
                    </div>

                    <div class="metric-card avg-time">
                        <div class="metric-value">
                            {{
                                selectedUser.averageTaskCompletionTimeHours.toFixed(
                                    1
                                )
                            }}
                        </div>
                        <div class="metric-label">Avg Hours</div>
                        <div class="metric-icon">
                            <i class="icon-clock"></i>
                        </div>
                    </div>
                </div>

                <!-- Task Distribution -->
                <div class="task-distribution">
                    <div class="distribution-item assigned">
                        <span class="distribution-count">{{
                            selectedUser.totalTasksAssigned
                        }}</span>
                        <span class="distribution-label">Assigned</span>
                    </div>
                    <div class="distribution-item completed">
                        <span class="distribution-count">{{
                            selectedUser.tasksCompleted
                        }}</span>
                        <span class="distribution-label">Completed</span>
                    </div>
                    <div class="distribution-item in-progress">
                        <span class="distribution-count">{{
                            selectedUser.tasksInProgress
                        }}</span>
                        <span class="distribution-label">In Progress</span>
                    </div>
                    <div class="distribution-item vetoed">
                        <span class="distribution-count">{{
                            selectedUser.tasksVetoed
                        }}</span>
                        <span class="distribution-label">Vetoed</span>
                    </div>
                </div>

                <!-- Productivity Chart -->
                <div class="productivity-section">
                    <div class="section-header">
                        <h5>Daily Productivity</h5>
                    </div>
                    <div
                        class="productivity-chart"
                        ref="productivityChartRef"
                    ></div>
                </div>
            </div>

            <!-- Team Summary (when "All Users" selected) -->
            <div v-else class="team-summary">
                <div class="summary-header">
                    <h4>Team Performance Summary</h4>
                    <span class="team-count">{{ data.length }} members</span>
                </div>

                <div class="team-metrics">
                    <div class="team-metric">
                        <span class="metric-value"
                            >{{ getTeamAverage("completionRate") }}%</span
                        >
                        <span class="metric-label">Avg Completion Rate</span>
                    </div>
                    <div class="team-metric">
                        <span class="metric-value"
                            >{{ getTeamAverage("vetoRate") }}%</span
                        >
                        <span class="metric-label">Avg Veto Rate</span>
                    </div>
                    <div class="team-metric">
                        <span class="metric-value">{{
                            getTeamTotal("tasksCompleted")
                        }}</span>
                        <span class="metric-label">Total Completed</span>
                    </div>
                </div>

                <div class="team-leaderboard">
                    <div class="leaderboard-header">
                        <h5>Top Performers</h5>
                    </div>
                    <div class="leaderboard-list">
                        <div
                            v-for="(user, index) in topPerformers"
                            :key="user.userId"
                            class="leaderboard-item"
                        >
                            <div class="rank">{{ index + 1 }}</div>
                            <div class="user-info">
                                <span class="user-name">{{
                                    user.userName
                                }}</span>
                                <span class="user-role">{{ user.role }}</span>
                            </div>
                            <div class="performance-score">
                                {{ user.completionRate.toFixed(1) }}%
                            </div>
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
import type { UserPerformanceDto } from "@/services/project/dashboard/dashboard.types";

interface Props {
    data?: UserPerformanceDto[];
    loading?: boolean;
    error?: string | null;
}

const props = defineProps<Props>();

// Refs
const productivityChartRef = ref<HTMLElement>();
const selectedUserId = ref<string>("");

// Computed properties
const selectedUser = computed(() => {
    if (!props.data?.length || !selectedUserId.value) return null;
    return props.data.find((u) => u.userId === selectedUserId.value) || null;
});

const topPerformers = computed(() => {
    if (!props.data?.length) return [];
    return [...props.data]
        .sort((a, b) => b.completionRate - a.completionRate)
        .slice(0, 5);
});

// Initialize selection
watch(
    () => props.data,
    (newData) => {
        if (newData?.length && !selectedUserId.value) {
            selectedUserId.value = newData[0].userId;
        }
    },
    { immediate: true }
);

// Methods
const getCompletionClass = (rate: number): string => {
    if (rate >= 90) return "excellent";
    if (rate >= 75) return "good";
    if (rate >= 50) return "fair";
    return "poor";
};

const getCompletionIcon = (rate: number): string => {
    if (rate >= 90) return "icon-check-circle";
    if (rate >= 75) return "icon-thumbs-up";
    if (rate >= 50) return "icon-minus-circle";
    return "icon-x-circle";
};

const getVetoClass = (rate: number): string => {
    if (rate <= 5) return "excellent";
    if (rate <= 15) return "good";
    if (rate <= 25) return "fair";
    return "poor";
};

const getVetoIcon = (rate: number): string => {
    if (rate <= 5) return "icon-shield-check";
    if (rate <= 15) return "icon-shield";
    if (rate <= 25) return "icon-alert-triangle";
    return "icon-alert-circle";
};

const formatDate = (date?: Date): string => {
    if (!date) return "Never";

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

const getTeamAverage = (field: keyof UserPerformanceDto): string => {
    if (!props.data?.length) return "0";
    const sum = props.data.reduce(
        (acc, user) => acc + (user[field] as number),
        0
    );
    return (sum / props.data.length).toFixed(1);
};

const getTeamTotal = (field: keyof UserPerformanceDto): number => {
    if (!props.data?.length) return 0;
    return props.data.reduce((acc, user) => acc + (user[field] as number), 0);
};

// D3 Chart Creation
const createProductivityChart = () => {
    if (
        !productivityChartRef.value ||
        !selectedUser.value?.dailyProductivity?.length
    )
        return;

    // Clear previous chart
    d3.select(productivityChartRef.value).selectAll("*").remove();

    const container = productivityChartRef.value;
    const containerRect = container.getBoundingClientRect();
    const margin = { top: 20, right: 10, bottom: 30, left: 30 };
    const width = containerRect.width - margin.left - margin.right;
    const height = 100 - margin.top - margin.bottom;

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
    const data = selectedUser.value.dailyProductivity
        .map((d) => ({
            ...d,
            date: new Date(d.date),
        }))
        .sort((a, b) => a.date.getTime() - b.date.getTime());

    // Scales
    const xScale = d3
        .scaleTime()
        .domain(d3.extent(data, (d) => d.date) as [Date, Date])
        .range([0, width]);

    const yScale = d3
        .scaleLinear()
        .domain([0, d3.max(data, (d) => d.tasksCompleted) || 1])
        .range([height, 0]);

    // Line generator
    const line = d3
        .line<any>()
        .x((d) => xScale(d.date))
        .y((d) => yScale(d.tasksCompleted))
        .curve(d3.curveMonotoneX);

    // Add line
    g.append("path")
        .datum(data)
        .attr("class", "productivity-line")
        .attr("d", line)
        .style("fill", "none")
        .style("stroke", "var(--color-primary)")
        .style("stroke-width", 2);

    // Add dots
    g.selectAll(".dot")
        .data(data)
        .enter()
        .append("circle")
        .attr("class", "dot")
        .attr("cx", (d) => xScale(d.date))
        .attr("cy", (d) => yScale(d.tasksCompleted))
        .attr("r", 3)
        .style("fill", "var(--color-primary)");

    // Add axes
    const xAxis = d3
        .axisBottom(xScale)
        .tickSize(-height)
        .tickFormat(d3.timeFormat("%m/%d") as any);

    g.append("g")
        .attr("class", "x-axis")
        .attr("transform", `translate(0,${height})`)
        .call(xAxis)
        .selectAll("text")
        .style("font-size", "9px")
        .style("fill", "var(--color-gray-600)");

    const yAxis = d3.axisLeft(yScale).tickSize(-width).ticks(4);

    g.append("g")
        .attr("class", "y-axis")
        .call(yAxis)
        .selectAll("text")
        .style("font-size", "9px")
        .style("fill", "var(--color-gray-600)");

    // Style grid lines
    g.selectAll(".tick line")
        .style("stroke", "var(--color-gray-300)")
        .style("stroke-dasharray", "2,2");

    g.selectAll(".domain").style("stroke", "var(--color-gray-300)");
};

// Watchers and lifecycle
watch(
    () => selectedUser.value,
    () => {
        if (selectedUser.value) {
            setTimeout(createProductivityChart, 100);
        }
    },
    { immediate: true }
);

onMounted(() => {
    const resizeObserver = new ResizeObserver(() => {
        createProductivityChart();
    });

    if (productivityChartRef.value) {
        resizeObserver.observe(productivityChartRef.value);
    }

    onUnmounted(() => {
        resizeObserver.disconnect();
    });
});
</script>

<style scoped>
.user-performance-widget {
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

.performance-content {
    display: flex;
    flex-direction: column;
    gap: 1rem;
    height: auto;
}

/* User Selector */
.user-selector {
    display: flex;
    justify-content: center;
}

.user-select {
    padding: 0.5rem 0.75rem;
    border: 1px solid var(--color-gray-300);
    border-radius: 6px;
    font-size: 0.75rem;
    background: var(--color-white);
    color: var(--color-gray-800);
    min-width: 150px;
}

.user-select:focus {
    outline: none;
    border-color: var(--color-primary);
    box-shadow: 0 0 0 2px rgba(0, 123, 255, 0.2);
}

/* User Overview */
.user-overview {
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.user-info {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.user-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
}

.user-header h4 {
    margin: 0;
    font-size: 0.875rem;
    font-weight: 600;
    color: var(--color-gray-800);
}

.user-role {
    font-size: 0.6875rem;
    font-weight: 500;
    color: var(--color-primary);
    background: var(--color-blue-100);
    padding: 0.125rem 0.375rem;
    border-radius: 4px;
}

.last-activity {
    font-size: 0.625rem;
    color: var(--color-gray-600);
}

/* Performance Metrics */
.performance-metrics {
    display: grid;
    grid-template-columns: repeat(4, 1fr);
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
    font-size: 1rem;
    font-weight: bold;
    color: var(--color-gray-800);
    margin-bottom: 2px;
}

.metric-label {
    font-size: 0.625rem;
    color: var(--color-gray-600);
}

.metric-indicator,
.metric-icon {
    position: absolute;
    top: 0.5rem;
    right: 0.5rem;
    font-size: 0.75rem;
}

.metric-indicator.excellent {
    color: var(--color-success);
}

.metric-indicator.good {
    color: var(--color-warning);
}

.metric-indicator.fair {
    color: var(--color-warning);
}

.metric-indicator.poor {
    color: var(--color-error);
}

.metric-icon {
    color: var(--color-primary);
}

/* Task Distribution */
.task-distribution {
    display: grid;
    grid-template-columns: repeat(4, 1fr);
    gap: 0.75rem;
}

.distribution-item {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 0.125rem;
    padding: 0.5rem;
    background: var(--color-gray-100);
    border-radius: 4px;
}

.distribution-count {
    font-size: 0.875rem;
    font-weight: bold;
    color: var(--color-gray-800);
}

.distribution-label {
    font-size: 0.5625rem;
    color: var(--color-gray-600);
}

.distribution-item.assigned .distribution-count {
    color: var(--color-primary);
}

.distribution-item.completed .distribution-count {
    color: var(--color-success);
}

.distribution-item.in-progress .distribution-count {
    color: var(--color-warning);
}

.distribution-item.vetoed .distribution-count {
    color: var(--color-error);
}

/* Productivity Section */
.productivity-section {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    flex: 1;
}

.section-header h5 {
    margin: 0;
    font-size: 0.75rem;
    font-weight: 600;
    color: var(--color-gray-800);
}

.productivity-chart {
    height: 100px;
    width: 100%;
    min-height: 100px;
}

/* Team Summary */
.team-summary {
    display: flex;
    flex-direction: column;
    gap: 1rem;
    height: 100%;
}

.summary-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
}

.summary-header h4 {
    margin: 0;
    font-size: 0.875rem;
    font-weight: 600;
    color: var(--color-gray-800);
}

.team-count {
    font-size: 11px;
    color: var(--color-gray-600);
}

.team-metrics {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 0.75rem;
}

.team-metric {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 0.125rem;
    padding: 0.75rem;
    background: var(--color-gray-100);
    border-radius: 6px;
}

.team-metric .metric-value {
    font-size: 1rem;
    font-weight: bold;
    color: var(--color-primary);
}

.team-metric .metric-label {
    font-size: 0.625rem;
    color: var(--color-gray-600);
    text-align: center;
}

/* Team Leaderboard */
.team-leaderboard {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    flex: 1;
}

.leaderboard-header h5 {
    margin: 0;
    font-size: 0.75rem;
    font-weight: 600;
    color: var(--color-gray-800);
}

.leaderboard-list {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
}

.leaderboard-item {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    padding: 0.5rem;
    background: var(--color-gray-100);
    border-radius: 4px;
}

.rank {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 20px;
    height: 20px;
    background: var(--color-primary);
    color: var(--color-white);
    border-radius: 50%;
    font-size: 0.625rem;
    font-weight: bold;
    flex-shrink: 0;
}

.leaderboard-item .user-info {
    flex: 1;
    display: flex;
    flex-direction: column;
    gap: 0.125rem;
}

.user-name {
    font-size: 0.6875rem;
    font-weight: 500;
    color: var(--color-gray-800);
}

.leaderboard-item .user-role {
    font-size: 0.5625rem;
    color: var(--color-gray-600);
    background: none;
    padding: 0;
}

.performance-score {
    font-size: 0.6875rem;
    font-weight: bold;
    color: var(--color-primary);
}

/* Responsive adjustments */
@media (max-width: 768px) {
    .performance-metrics {
        grid-template-columns: repeat(2, 1fr);
    }

    .task-distribution {
        grid-template-columns: repeat(2, 1fr);
    }

    .team-metrics {
        grid-template-columns: 1fr;
    }
}
</style>
