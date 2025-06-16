import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { labelSchemeService } from '../labelSchemeService';
import type { LabelSchemeResponse } from '@/types/label/responses';
import type { CreateLabelSchemeRequest, UpdateLabelSchemeRequest } from '@/types/label/requests';
import type { PaginatedResponse } from '@/types/api/paginatedResponse';

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

import apiClient from '../apiClient';

// Get references to the mocked functions
const mockApiClient = apiClient as any;
const mockGet = mockApiClient.get;
const mockPost = mockApiClient.post;
const mockPut = mockApiClient.put;
const mockDelete = mockApiClient.delete;

describe('LabelSchemeService', () => {
    beforeEach(() => {
        vi.clearAllMocks();
    });

    afterEach(() => {
        vi.restoreAllMocks();
    });

    describe('getLabelSchemesForProject', () => {
        it('should fetch label schemes for a project successfully', async () => {
            const projectId = 1;
            const mockResponse: PaginatedResponse<LabelSchemeResponse> = {
                data: [
                    {
                        id: 1,
                        name: 'Test Scheme',
                        description: 'A test label scheme',
                        projectId: 1,
                        isDefault: true,
                        createdAt: '2025-01-01T00:00:00Z',
                        updatedAt: '2025-01-01T00:00:00Z',
                        labels: []
                    }
                ],
                pageSize: 25,
                currentPage: 1,
                totalPages: 1,
                totalItems: 1
            };

            mockGet.mockResolvedValue({ data: mockResponse });

            const result = await labelSchemeService.getLabelSchemesForProject(projectId);

            expect(mockGet).toHaveBeenCalledWith(
                `/projects/${projectId}/labelschemes`,
                { params: {} }
            );
            expect(result.data).toHaveLength(1);
            expect(result.data[0]).toEqual({
                labelSchemeId: 1,
                name: 'Test Scheme',
                description: 'A test label scheme',
                projectId: 1,
                isDefault: true,
                createdAt: '2025-01-01T00:00:00Z'
            });
            expect(result.currentPage).toBe(1);
            expect(result.pageSize).toBe(25);
            expect(result.totalPages).toBe(1);
            expect(result.totalItems).toBe(1);
        });

        it('should handle query parameters', async () => {
            const projectId = 1;
            const query = {
                filterOn: 'name',
                filterQuery: 'test',
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

            await labelSchemeService.getLabelSchemesForProject(projectId, query);

            expect(mockGet).toHaveBeenCalledWith(
                `/projects/${projectId}/labelschemes`,
                { params: query }
            );
        });

        it('should throw error when API call fails', async () => {
            const projectId = 1;
            const error = new Error('API Error');

            mockGet.mockRejectedValue(error);

            await expect(
                labelSchemeService.getLabelSchemesForProject(projectId)
            ).rejects.toThrow('API Error');
        });
    });

    describe('getLabelSchemeById', () => {
        it('should fetch a single label scheme successfully', async () => {
            const projectId = 1;
            const schemeId = 1;
            const mockResponse: LabelSchemeResponse = {
                id: 1,
                name: 'Test Scheme',
                description: 'A test label scheme',
                projectId: 1,
                isDefault: true,
                createdAt: '2025-01-01T00:00:00Z',
                updatedAt: '2025-01-01T00:00:00Z',
                labels: []
            };

            mockGet.mockResolvedValue({ data: mockResponse });

            const result = await labelSchemeService.getLabelSchemeById(projectId, schemeId);

            expect(mockGet).toHaveBeenCalledWith(
                `/projects/${projectId}/labelschemes/${schemeId}`
            );
            expect(result).toEqual({
                labelSchemeId: 1,
                name: 'Test Scheme',
                description: 'A test label scheme',
                projectId: 1,
                isDefault: true,
                createdAt: '2025-01-01T00:00:00Z'
            });
        });

        it('should throw error when label scheme not found', async () => {
            const projectId = 1;
            const schemeId = 999;
            const error = new Error('Not Found');

            mockGet.mockRejectedValue(error);

            await expect(
                labelSchemeService.getLabelSchemeById(projectId, schemeId)
            ).rejects.toThrow('Not Found');
        });
    });

    describe('createLabelScheme', () => {
        it('should create a new label scheme successfully', async () => {
            const projectId = 1;
            const createData: CreateLabelSchemeRequest = {
                name: 'New Scheme',
                description: 'A new label scheme'
            };
            const mockResponse: LabelSchemeResponse = {
                id: 2,
                name: 'New Scheme',
                description: 'A new label scheme',
                projectId: 1,
                isDefault: false,
                createdAt: '2025-01-01T00:00:00Z',
                updatedAt: '2025-01-01T00:00:00Z',
                labels: []
            };

            mockPost.mockResolvedValue({ data: mockResponse });

            const result = await labelSchemeService.createLabelScheme(projectId, createData);

            expect(mockPost).toHaveBeenCalledWith(
                `/projects/${projectId}/labelschemes`,
                createData
            );
            expect(result).toEqual({
                labelSchemeId: 2,
                name: 'New Scheme',
                description: 'A new label scheme',
                projectId: 1,
                isDefault: false,
                createdAt: '2025-01-01T00:00:00Z'
            });
        });

        it('should throw error when creation fails', async () => {
            const projectId = 1;
            const createData: CreateLabelSchemeRequest = {
                name: 'New Scheme'
            };
            const error = new Error('Creation failed');

            mockPost.mockRejectedValue(error);

            await expect(
                labelSchemeService.createLabelScheme(projectId, createData)
            ).rejects.toThrow('Creation failed');
        });
    });

    describe('updateLabelScheme', () => {
        it('should update a label scheme successfully', async () => {
            const projectId = 1;
            const schemeId = 1;
            const updateData: UpdateLabelSchemeRequest = {
                name: 'Updated Scheme',
                description: 'An updated description'
            };
            const mockResponse: LabelSchemeResponse = {
                id: 1,
                name: 'Updated Scheme',
                description: 'An updated description',
                projectId: 1,
                isDefault: true,
                createdAt: '2025-01-01T00:00:00Z',
                updatedAt: '2025-01-01T01:00:00Z',
                labels: []
            };

            mockPut.mockResolvedValue({ data: mockResponse });

            const result = await labelSchemeService.updateLabelScheme(projectId, schemeId, updateData);

            expect(mockPut).toHaveBeenCalledWith(
                `/projects/${projectId}/labelschemes/${schemeId}`,
                updateData
            );
            expect(result.name).toBe('Updated Scheme');
            expect(result.description).toBe('An updated description');
        });

        it('should throw error when update fails', async () => {
            const projectId = 1;
            const schemeId = 1;
            const updateData: UpdateLabelSchemeRequest = {
                name: 'Updated Scheme'
            };
            const error = new Error('Update failed');

            mockPut.mockRejectedValue(error);

            await expect(
                labelSchemeService.updateLabelScheme(projectId, schemeId, updateData)
            ).rejects.toThrow('Update failed');
        });
    });

    describe('deleteLabelScheme', () => {
        it('should delete a label scheme successfully', async () => {
            const projectId = 1;
            const schemeId = 1;

            mockDelete.mockResolvedValue({});

            await labelSchemeService.deleteLabelScheme(projectId, schemeId);

            expect(mockDelete).toHaveBeenCalledWith(
                `/projects/${projectId}/labelschemes/${schemeId}`
            );
        });

        it('should throw error when deletion fails', async () => {
            const projectId = 1;
            const schemeId = 1;
            const error = new Error('Deletion failed');

            mockDelete.mockRejectedValue(error);

            await expect(
                labelSchemeService.deleteLabelScheme(projectId, schemeId)
            ).rejects.toThrow('Deletion failed');
        });
    });
});
