import { describe, it, expect } from 'vitest';
import { clampPointToImageBounds, isPointInImageBounds } from '../geometry';

describe('Geometry - Boundary Validation', () => {
    const imageWidth = 800;
    const imageHeight = 600;

    describe('clampPointToImageBounds', () => {
        it('should not change point that is within bounds', () => {
            const point = { x: 400, y: 300 };
            const result = clampPointToImageBounds(point, imageWidth, imageHeight);
            expect(result).toEqual({ x: 400, y: 300 });
        });

        it('should clamp point that is out of left bound', () => {
            const point = { x: -50, y: 300 };
            const result = clampPointToImageBounds(point, imageWidth, imageHeight);
            expect(result).toEqual({ x: 0, y: 300 });
        });

        it('should clamp point that is out of right bound', () => {
            const point = { x: 850, y: 300 };
            const result = clampPointToImageBounds(point, imageWidth, imageHeight);
            expect(result).toEqual({ x: 800, y: 300 });
        });

        it('should clamp point that is out of top bound', () => {
            const point = { x: 400, y: -30 };
            const result = clampPointToImageBounds(point, imageWidth, imageHeight);
            expect(result).toEqual({ x: 400, y: 0 });
        });

        it('should clamp point that is out of bottom bound', () => {
            const point = { x: 400, y: 650 };
            const result = clampPointToImageBounds(point, imageWidth, imageHeight);
            expect(result).toEqual({ x: 400, y: 600 });
        });

        it('should clamp point that is out of multiple bounds', () => {
            const point = { x: -10, y: -20 };
            const result = clampPointToImageBounds(point, imageWidth, imageHeight);
            expect(result).toEqual({ x: 0, y: 0 });
        });

        it('should clamp point that is far out of bounds', () => {
            const point = { x: 1000, y: 800 };
            const result = clampPointToImageBounds(point, imageWidth, imageHeight);
            expect(result).toEqual({ x: 800, y: 600 });
        });
    });

    describe('isPointInImageBounds', () => {
        it('should return true for point within bounds', () => {
            const point = { x: 400, y: 300 };
            const result = isPointInImageBounds(point, imageWidth, imageHeight);
            expect(result).toBe(true);
        });

        it('should return true for point on boundary', () => {
            const point = { x: 0, y: 0 };
            const result = isPointInImageBounds(point, imageWidth, imageHeight);
            expect(result).toBe(true);
        });

        it('should return true for point on opposite boundary', () => {
            const point = { x: 800, y: 600 };
            const result = isPointInImageBounds(point, imageWidth, imageHeight);
            expect(result).toBe(true);
        });

        it('should return false for point out of left bound', () => {
            const point = { x: -1, y: 300 };
            const result = isPointInImageBounds(point, imageWidth, imageHeight);
            expect(result).toBe(false);
        });

        it('should return false for point out of right bound', () => {
            const point = { x: 801, y: 300 };
            const result = isPointInImageBounds(point, imageWidth, imageHeight);
            expect(result).toBe(false);
        });

        it('should return false for point out of top bound', () => {
            const point = { x: 400, y: -1 };
            const result = isPointInImageBounds(point, imageWidth, imageHeight);
            expect(result).toBe(false);
        });

        it('should return false for point out of bottom bound', () => {
            const point = { x: 400, y: 601 };
            const result = isPointInImageBounds(point, imageWidth, imageHeight);
            expect(result).toBe(false);
        });
    });
});