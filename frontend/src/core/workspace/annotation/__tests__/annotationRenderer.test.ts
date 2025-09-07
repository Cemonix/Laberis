import { describe, it, expect } from 'vitest';
import { calculateRenderSizes } from '../annotationRenderer';

describe('AnnotationRenderer - Scaling', () => {
    describe('calculateRenderSizes', () => {
        it('should scale down line width when zoomed in', () => {
            const zoomLevel = 2; // Zoomed in 2x
            const result = calculateRenderSizes(zoomLevel);
            
            expect(result.lineWidth).toBe(1); // 2 * (1/2) = 1
            expect(result.thinLineWidth).toBe(0.5); // 1 * (1/2) = 0.5
            expect(result.pointRadius).toBe(2); // 4 * (1/2) = 2
            expect(result.handleSize).toBe(4); // 8 * (1/2) = 4
        });

        it('should scale up line width when zoomed out (within limits)', () => {
            const zoomLevel = 0.5; // Zoomed out 2x
            const result = calculateRenderSizes(zoomLevel);
            
            // scaleFactor = Math.min(1/0.5, 2) = Math.min(2, 2) = 2
            expect(result.lineWidth).toBe(4); // Math.max(2 * 2, 1) = Math.max(4, 1) = 4
            expect(result.thinLineWidth).toBe(2); // Math.max(1 * 2, 0.5) = Math.max(2, 0.5) = 2
            expect(result.pointRadius).toBe(8); // 4 * 2 = 8
            expect(result.handleSize).toBe(16); // Math.max(8 * 2, 4) = Math.max(16, 4) = 16
        });

        it('should handle extreme zoom out with maximum scale factor', () => {
            const zoomLevel = 0.1; // Extremely zoomed out
            const result = calculateRenderSizes(zoomLevel);
            
            // scaleFactor = Math.min(1/0.1, 2) = Math.min(10, 2) = 2
            expect(result.lineWidth).toBe(4); // Math.max(2 * 2, 1) = Math.max(4, 1) = 4
            expect(result.thinLineWidth).toBe(2); // Math.max(1 * 2, 0.5) = Math.max(2, 0.5) = 2
            expect(result.pointRadius).toBe(8); // 4 * 2 = 8
            expect(result.handleSize).toBe(16); // Math.max(8 * 2, 4) = Math.max(16, 4) = 16
        });

        it('should not change sizes at zoom level 1', () => {
            const zoomLevel = 1; // Normal zoom
            const result = calculateRenderSizes(zoomLevel);
            
            expect(result.lineWidth).toBe(2); // Base line width
            expect(result.thinLineWidth).toBe(1); // Base thin line width
            expect(result.pointRadius).toBe(4); // Base point radius
            expect(result.handleSize).toBe(8); // Base handle size
        });

        it('should handle very high zoom levels', () => {
            const zoomLevel = 10; // Very zoomed in
            const result = calculateRenderSizes(zoomLevel);
            
            expect(result.lineWidth).toBe(1); // Math.max(2 * (1/10), 1) = 1
            expect(result.thinLineWidth).toBe(0.5); // Math.max(1 * (1/10), 0.5) = 0.5
            expect(result.pointRadius).toBe(0.4); // 4 * (1/10) = 0.4
            expect(result.handleSize).toBe(4); // Math.max(8 * (1/10), 4) = 4
        });

        it('should ensure minimum values are respected', () => {
            const zoomLevel = 100; // Extremely zoomed in
            const result = calculateRenderSizes(zoomLevel);
            
            // Check that minimums are enforced
            expect(result.lineWidth).toBeGreaterThanOrEqual(1);
            expect(result.thinLineWidth).toBeGreaterThanOrEqual(0.5);
            expect(result.handleSize).toBeGreaterThanOrEqual(4);
            // pointRadius can go below its base value when zoomed in
        });
    });
});