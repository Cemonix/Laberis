import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { labelService } from '../labelService';
import type { LabelResponse, CreateLabelRequest, UpdateLabelRequest } from '../label.types';
import type { PaginatedResponse } from '@/services/base/paginatedResponse';

// Mock the API client
vi.mock('../apiClient', () => ({
    default: {
        get: vi.fn(),
        post: vi.fn(),
        put: vi.fn(),
        delete: vi.fn(),
    }
}));

// Mock the logger
vi.mock('@/utils/logger', () => ({
    AppLogger: {
        createServiceLogger: vi.fn(() => ({
            info: vi.fn(),
            error: vi.fn(),
            debug: vi.fn(),
            warn: vi.fn(),
        }))
    }
}));

import apiClient from '../../../apiClient';

// Get references to the mocked functions
const mockApiClient = apiClient as any;
const mockGet = mockApiClient.get;
const mockPost = mockApiClient.post;
const mockPut = mockApiClient.put;
const mockDelete = mockApiClient.delete;

describe('LabelService', () => {
    beforeEach(() => {
        vi.clearAllMocks();
    });

    afterEach(() => {
        vi.restoreAllMocks();
    });

    describe('getLabelsForScheme', () => {
        it('should fetch labels for a scheme successfully', async () => {
            const projectId = 1;
            const schemeId = 1;
            const mockResponse: PaginatedResponse<LabelResponse> = {
                data: [
                    {
                        id: 1,
                        name: 'Person',
                        color: '#FF0000',
                        description: 'Person label',
                        labelSchemeId: 1,
                        metadata: { category: 'entity' },
                        createdAt: '2025-01-01T00:00:00Z',
                        updatedAt: '2025-01-01T00:00:00Z'
                    },
                    {
                        id: 2,
                        name: 'Vehicle',
                        color: '#00FF00',
                        description: 'Vehicle label',
                        labelSchemeId: 1,
                        metadata: null,
                        createdAt: '2025-01-01T00:00:00Z',
                        updatedAt: '2025-01-01T00:00:00Z'
                    }
                ],
                pageSize: 25,
                currentPage: 1,
                totalPages: 1,
                totalItems: 2
            };

            mockGet.mockResolvedValue({ data: mockResponse });

            const result = await labelService.getLabelsForScheme(projectId, schemeId);

            expect(mockGet).toHaveBeenCalledWith(
                `/projects/${projectId}/labelschemes/${schemeId}/labels`,
                { params: {} }
            );
            expect(result.data).toHaveLength(2);
            expect(result.data[0]).toEqual({
                labelId: 1,
                name: 'Person',
                color: '#FF0000',
                description: 'Person label',
                labelSchemeId: 1,
                createdAt: '2025-01-01T00:00:00Z'
            });
            expect(result.currentPage).toBe(1);
            expect(result.pageSize).toBe(25);
            expect(result.totalPages).toBe(1);
            expect(result.totalItems).toBe(2);
        });

        it('should handle query parameters', async () => {
            const projectId = 1;
            const schemeId = 1;
            const query = {
                filterOn: 'name',
                filterQuery: 'person',
                sortBy: 'name',
                isAscending: true,
                pageNumber: 2,
                pageSize: 10
            };

            mockGet.mockResolvedValue({ 
                data: { 
                    data: [], 
                    pageSize: 10, 
                    currentPage: 2, 
                    totalPages: 0,
                    totalItems: 0
                } 
            });

            await labelService.getLabelsForScheme(projectId, schemeId, query);

            expect(mockGet).toHaveBeenCalledWith(
                `/projects/${projectId}/labelschemes/${schemeId}/labels`,
                { params: query }
            );
        });

        it('should throw error when API call fails', async () => {
            const projectId = 1;
            const schemeId = 1;
            const error = new Error('API Error');

            mockGet.mockRejectedValue(error);

            await expect(
                labelService.getLabelsForScheme(projectId, schemeId)
            ).rejects.toThrow('API Error');
        });
    });

    describe('getLabelById', () => {
        it('should fetch a single label successfully', async () => {
            const projectId = 1;
            const schemeId = 1;
            const labelId = 1;
            const mockResponse: LabelResponse = {
                id: 1,
                name: 'Person',
                color: '#FF0000',
                description: 'Person label',
                labelSchemeId: 1,
                metadata: { category: 'entity' },
                createdAt: '2025-01-01T00:00:00Z',
                updatedAt: '2025-01-01T00:00:00Z'
            };

            mockGet.mockResolvedValue({ data: mockResponse });

            const result = await labelService.getLabelById(projectId, schemeId, labelId);

            expect(mockGet).toHaveBeenCalledWith(
                `/projects/${projectId}/labelschemes/${schemeId}/labels/${labelId}`,
                { params: undefined }
            );
            expect(result).toEqual({
                labelId: 1,
                name: 'Person',
                color: '#FF0000',
                description: 'Person label',
                labelSchemeId: 1,
                createdAt: '2025-01-01T00:00:00Z'
            });
        });

        it('should throw error when label not found', async () => {
            const projectId = 1;
            const schemeId = 1;
            const labelId = 999;
            const error = new Error('Not Found');

            mockGet.mockRejectedValue(error);

            await expect(
                labelService.getLabelById(projectId, schemeId, labelId)
            ).rejects.toThrow('Not Found');
        });
    });

    describe('createLabel', () => {
        it('should create a new label successfully', async () => {
            const projectId = 1;
            const schemeId = 1;
            const createData: CreateLabelRequest = {
                name: 'Animal',
                color: '#0000FF',
                description: 'Animal label'
            };
            const mockResponse: LabelResponse = {
                id: 3,
                name: 'Animal',
                color: '#0000FF',
                description: 'Animal label',
                labelSchemeId: 1,
                metadata: null,
                createdAt: '2025-01-01T00:00:00Z',
                updatedAt: '2025-01-01T00:00:00Z'
            };

            mockPost.mockResolvedValue({ data: mockResponse });

            const result = await labelService.createLabel(projectId, schemeId, createData);

            expect(mockPost).toHaveBeenCalledWith(
                `/projects/${projectId}/labelschemes/${schemeId}/labels`,
                createData
            );
            expect(result).toEqual({
                labelId: 3,
                name: 'Animal',
                color: '#0000FF',
                description: 'Animal label',
                labelSchemeId: 1,
                createdAt: '2025-01-01T00:00:00Z'
            });
        });

        it('should create label with minimal data', async () => {
            const projectId = 1;
            const schemeId = 1;
            const createData: CreateLabelRequest = {
                name: 'Simple Label',
                color: '#FFFF00'
            };
            const mockResponse: LabelResponse = {
                id: 4,
                name: 'Simple Label',
                color: '#FFFF00',
                description: undefined,
                labelSchemeId: 1,
                metadata: null,
                createdAt: '2025-01-01T00:00:00Z',
                updatedAt: '2025-01-01T00:00:00Z'
            };

            mockPost.mockResolvedValue({ data: mockResponse });

            const result = await labelService.createLabel(projectId, schemeId, createData);

            expect(result.name).toBe('Simple Label');
            expect(result.description).toBeUndefined();
        });

        it('should throw error when creation fails', async () => {
            const projectId = 1;
            const schemeId = 1;
            const createData: CreateLabelRequest = {
                name: 'Invalid Label',
                color: '#FF0000'
            };
            const error = new Error('Creation failed');

            mockPost.mockRejectedValue(error);

            await expect(
                labelService.createLabel(projectId, schemeId, createData)
            ).rejects.toThrow('Creation failed');
        });
    });

    describe('updateLabel', () => {
        it('should update a label successfully', async () => {
            const projectId = 1;
            const schemeId = 1;
            const labelId = 1;
            const updateData: UpdateLabelRequest = {
                name: 'Updated Person',
                description: 'Updated person description',
                color: '#FF00FF'
            };
            const mockResponse: LabelResponse = {
                id: 1,
                name: 'Updated Person',
                color: '#FF00FF',
                description: 'Updated person description',
                labelSchemeId: 1,
                metadata: { category: 'entity' },
                createdAt: '2025-01-01T00:00:00Z',
                updatedAt: '2025-01-01T01:00:00Z'
            };

            mockPut.mockResolvedValue({ data: mockResponse });

            const result = await labelService.updateLabel(projectId, schemeId, labelId, updateData);

            expect(mockPut).toHaveBeenCalledWith(
                `/projects/${projectId}/labelschemes/${schemeId}/labels/${labelId}`,
                updateData
            );
            expect(result.name).toBe('Updated Person');
            expect(result.description).toBe('Updated person description');
            expect(result.color).toBe('#FF00FF');
        });

        it('should update only specific fields', async () => {
            const projectId = 1;
            const schemeId = 1;
            const labelId = 1;
            const updateData: UpdateLabelRequest = {
                name: 'New Name Only'
            };
            const mockResponse: LabelResponse = {
                id: 1,
                name: 'New Name Only',
                color: '#FF0000',
                description: 'Original description',
                labelSchemeId: 1,
                metadata: null,
                createdAt: '2025-01-01T00:00:00Z',
                updatedAt: '2025-01-01T01:00:00Z'
            };

            mockPut.mockResolvedValue({ data: mockResponse });

            const result = await labelService.updateLabel(projectId, schemeId, labelId, updateData);

            expect(result.name).toBe('New Name Only');
            expect(result.description).toBe('Original description');
        });

        it('should throw error when update fails', async () => {
            const projectId = 1;
            const schemeId = 1;
            const labelId = 1;
            const updateData: UpdateLabelRequest = {
                name: 'Failed Update'
            };
            const error = new Error('Update failed');

            mockPut.mockRejectedValue(error);

            await expect(
                labelService.updateLabel(projectId, schemeId, labelId, updateData)
            ).rejects.toThrow('Update failed');
        });
    });

    describe('deleteLabel', () => {
        it('should delete a label successfully', async () => {
            const projectId = 1;
            const schemeId = 1;
            const labelId = 1;

            mockDelete.mockResolvedValue({ status: 204 });

            await labelService.deleteLabel(projectId, schemeId, labelId);

            expect(mockDelete).toHaveBeenCalledWith(
                `/projects/${projectId}/labelschemes/${schemeId}/labels/${labelId}`
            );
        });

        it('should throw error when deletion fails', async () => {
            const projectId = 1;
            const schemeId = 1;
            const labelId = 1;
            const error = new Error('Deletion failed');

            mockDelete.mockRejectedValue(error);

            await expect(
                labelService.deleteLabel(projectId, schemeId, labelId)
            ).rejects.toThrow('Deletion failed');
        });
    });

    describe('Error handling', () => {
        it('should handle network errors', async () => {
            const networkError = new Error('Network Error');
            mockGet.mockRejectedValue(networkError);

            await expect(
                labelService.getLabelsForScheme(1, 1)
            ).rejects.toThrow('Network Error');
        });

        it('should handle validation errors', async () => {
            const validationError = new Error('Validation Error');
            mockPost.mockRejectedValue(validationError);

            await expect(
                labelService.createLabel(1, 1, { name: '', color: '#FF0000' })
            ).rejects.toThrow('Validation Error');
        });
    });
});
