<template>
    <div class="bar-chart-container">
        <svg ref="svgRef" :width="width" :height="height"></svg>
    </div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch, nextTick } from 'vue';
import * as d3 from 'd3';

interface ChartData {
    label: string;
    value: number;
}

interface Props {
    data: ChartData[];
    width?: number;
    height?: number;
    margin?: { top: number; right: number; bottom: number; left: number };
    color?: string;
}

const props = withDefaults(defineProps<Props>(), {
    width: 350,
    height: 200,
    margin: () => ({ top: 20, right: 30, bottom: 60, left: 60 }),
    color: '#3b82f6'
});

const svgRef = ref<SVGElement>();

const drawChart = () => {
    if (!svgRef.value || !props.data.length) return;

    // Clear previous chart
    d3.select(svgRef.value).selectAll("*").remove();

    const svg = d3.select(svgRef.value);
    const chartWidth = props.width - props.margin.left - props.margin.right;
    const chartHeight = props.height - props.margin.top - props.margin.bottom;

    const g = svg.append("g")
        .attr("transform", `translate(${props.margin.left},${props.margin.top})`);

    // Scales
    const xScale = d3.scaleBand()
        .domain(props.data.map(d => d.label))
        .range([0, chartWidth])
        .padding(0.2);

    const yScale = d3.scaleLinear()
        .domain([0, d3.max(props.data, d => d.value) || 0])
        .range([chartHeight, 0]);

    // Axes
    g.append("g")
        .attr("class", "x-axis")
        .attr("transform", `translate(0,${chartHeight})`)
        .call(d3.axisBottom(xScale))
        .selectAll("text")
        .style("text-anchor", "end")
        .attr("dx", "-.8em")
        .attr("dy", ".15em")
        .attr("transform", "rotate(-45)")
        .style("font-size", "12px")
        .style("fill", "#666");

    g.append("g")
        .attr("class", "y-axis")
        .call(d3.axisLeft(yScale))
        .selectAll("text")
        .style("font-size", "12px")
        .style("fill", "#666");

    // Bars
    g.selectAll(".bar")
        .data(props.data)
        .enter().append("rect")
        .attr("class", "bar")
        .attr("x", d => xScale(d.label)!)
        .attr("width", xScale.bandwidth())
        .attr("y", chartHeight)
        .attr("height", 0)
        .attr("fill", props.color)
        .attr("rx", 4)
        .attr("ry", 4)
        .on("mouseover", function() {
            d3.select(this)
                .transition()
                .duration(200)
                .attr("fill", d3.color(props.color)!.darker(0.3).toString());
        })
        .on("mouseout", function() {
            d3.select(this)
                .transition()
                .duration(200)
                .attr("fill", props.color);
        })
        .transition()
        .duration(800)
        .attr("y", d => yScale(d.value))
        .attr("height", d => chartHeight - yScale(d.value));

    // Value labels on bars
    g.selectAll(".bar-label")
        .data(props.data)
        .enter().append("text")
        .attr("class", "bar-label")
        .attr("x", d => xScale(d.label)! + xScale.bandwidth() / 2)
        .attr("y", d => yScale(d.value) - 5)
        .attr("text-anchor", "middle")
        .style("font-size", "12px")
        .style("font-weight", "bold")
        .style("fill", "#333")
        .style("opacity", 0)
        .text(d => d.value)
        .transition()
        .delay(800)
        .duration(300)
        .style("opacity", 1);
};

onMounted(() => {
    nextTick(() => {
        drawChart();
    });
});

watch(() => props.data, () => {
    drawChart();
}, { deep: true });
</script>

<style lang="scss" scoped>
.bar-chart-container {
    display: flex;
    justify-content: center;
    align-items: center;
    width: 100%;
    height: 100%;
}

:deep(.x-axis .domain),
:deep(.y-axis .domain) {
    stroke: #ccc;
}

:deep(.x-axis .tick line),
:deep(.y-axis .tick line) {
    stroke: #eee;
}
</style>
