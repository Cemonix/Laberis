import { describe, it, expect, beforeEach, vi, afterEach } from "vitest";
import { AssetManager } from "../assetManager";
import { type Asset, AssetStatus } from '@/core/asset/asset.types';

// Mock the services
vi.mock("@/services/projects", () => ({
    assetService: {
        getAssetById: vi.fn(),
    },
}));

vi.mock("@/utils/logger", () => ({
    AppLogger: {
        createServiceLogger: vi.fn(() => ({
            info: vi.fn(),
            error: vi.fn(),
            warn: vi.fn(),
            debug: vi.fn()
        }))
    }
}));

import { assetService } from "@/services/project";

describe("AssetManager", () => {
    let assetManager: AssetManager;

    const mockAsset: Asset = {
        id: 1,
        filename: "test-image.jpg",
        imageUrl: "https://example.com/test-image.jpg",
        width: 800,
        height: 600,
        mimeType: "image/jpeg",
        sizeBytes: 1024000,
        durationMs: 0,
        status: AssetStatus.READY_FOR_ANNOTATION,
        createdAt: "2024-01-01T00:00:00Z",
        updatedAt: "2024-01-01T00:00:00Z"
    };

    beforeEach(() => {
        assetManager = new AssetManager();
        vi.clearAllMocks();
    });

    afterEach(() => {
        vi.restoreAllMocks();
    });

    describe("Constructor", () => {
        it("should create AssetManager instance", () => {
            expect(assetManager).toBeInstanceOf(AssetManager);
        });
    });

    describe("loadAsset", () => {
        it("should successfully load asset with valid parameters", async () => {
            // Arrange
            const projectId = "123";
            const assetId = "456";
            
            vi.mocked(assetService.getAssetById).mockResolvedValue(mockAsset);

            // Act
            const result = await assetManager.loadAsset(projectId, assetId);

            // Assert
            expect(result.success).toBe(true);
            if (result.success) {
                expect(result.asset).toEqual(mockAsset);
                expect(result.imageUrl).toBe(mockAsset.imageUrl);
                expect(result.naturalDimensions).toEqual({
                    width: mockAsset.width,
                    height: mockAsset.height
                });
            }

            expect(assetService.getAssetById).toHaveBeenCalledWith(123, 456);
            expect(assetService.getAssetById).toHaveBeenCalledTimes(1);
        });

        it("should handle invalid project ID", async () => {
            // Arrange
            const projectId = "invalid";
            const assetId = "456";

            // Act
            const result = await assetManager.loadAsset(projectId, assetId);

            // Assert
            expect(result.success).toBe(false);
            if (!result.success) {
                expect(result.error).toContain("Invalid project ID");
            }

            expect(assetService.getAssetById).not.toHaveBeenCalled();
        });

        it("should handle invalid asset ID", async () => {
            // Arrange
            const projectId = "123";
            const assetId = "invalid";

            // Act
            const result = await assetManager.loadAsset(projectId, assetId);

            // Assert
            expect(result.success).toBe(false);
            if (!result.success) {
                expect(result.error).toContain("Invalid asset ID");
            }

            expect(assetService.getAssetById).not.toHaveBeenCalled();
        });

        it("should handle null/undefined parameters", async () => {
            // Test null projectId
            let result = await assetManager.loadAsset(null as any, "456");
            expect(result.success).toBe(false);

            // Test undefined assetId
            result = await assetManager.loadAsset("123", undefined as any);
            expect(result.success).toBe(false);

            // Test empty strings
            result = await assetManager.loadAsset("", "456");
            expect(result.success).toBe(false);

            result = await assetManager.loadAsset("123", "");
            expect(result.success).toBe(false);
        });

        it("should handle API service errors", async () => {
            // Arrange
            const projectId = "123";
            const assetId = "456";
            const apiError = new Error("Asset not found");
            
            vi.mocked(assetService.getAssetById).mockRejectedValue(apiError);

            // Act
            const result = await assetManager.loadAsset(projectId, assetId);

            // Assert
            expect(result.success).toBe(false);
            if (!result.success) {
                expect(result.error).toBe("Asset not found");
            }

            expect(assetService.getAssetById).toHaveBeenCalledWith(123, 456);
        });

        it("should handle asset with missing image dimensions", async () => {
            // Arrange
            const assetWithoutDimensions = {
                ...mockAsset,
                width: undefined,
                height: undefined
            } as unknown as Asset;
            
            vi.mocked(assetService.getAssetById).mockResolvedValue(assetWithoutDimensions);

            // Act
            const result = await assetManager.loadAsset("123", "456");

            // Assert
            expect(result.success).toBe(true);
            if (result.success) {
                expect(result.naturalDimensions).toBeNull();
            }
        });

        it("should handle asset with missing imageUrl", async () => {
            // Arrange
            const assetWithoutUrl = {
                ...mockAsset,
                imageUrl: undefined
            } as Asset;
            
            vi.mocked(assetService.getAssetById).mockResolvedValue(assetWithoutUrl);

            // Act
            const result = await assetManager.loadAsset("123", "456");

            // Assert
            expect(result.success).toBe(true);
            if (result.success) {
                expect(result.imageUrl).toBeNull();
            }
        });
    });

    describe("validateAssetData", () => {
        it("should validate correct asset data", () => {
            // Act
            const result = assetManager.validateAssetData(mockAsset);

            // Assert
            expect(result.isValid).toBe(true);
            expect(result.errors).toHaveLength(0);
        });

        it("should reject asset without required fields", () => {
            // Arrange
            const invalidAsset = {
                ...mockAsset,
                id: undefined,
                filename: undefined
            } as any;

            // Act
            const result = assetManager.validateAssetData(invalidAsset);

            // Assert
            expect(result.isValid).toBe(false);
            expect(result.errors.length).toBeGreaterThan(0);
            expect(result.errors.some(error => error.includes('id'))).toBe(true);
            expect(result.errors.some(error => error.includes('filename'))).toBe(true);
        });

        it("should reject asset with invalid dimensions", () => {
            // Arrange
            const invalidAsset = {
                ...mockAsset,
                width: -100,
                height: 0
            };

            // Act
            const result = assetManager.validateAssetData(invalidAsset);

            // Assert
            expect(result.isValid).toBe(false);
            expect(result.errors.some(error => error.includes('width'))).toBe(true);
            expect(result.errors.some(error => error.includes('height'))).toBe(true);
        });

        it("should reject asset with invalid file size", () => {
            // Arrange
            const invalidAsset = {
                ...mockAsset,
                sizeBytes: -1
            };

            // Act
            const result = assetManager.validateAssetData(invalidAsset);

            // Assert
            expect(result.isValid).toBe(false);
            expect(result.errors.some(error => error.includes('sizeBytes'))).toBe(true);
        });

        it("should handle null/undefined asset", () => {
            // Test null
            let result = assetManager.validateAssetData(null as any);
            expect(result.isValid).toBe(false);
            expect(result.errors.some(error => error.includes('Asset is required'))).toBe(true);

            // Test undefined
            result = assetManager.validateAssetData(undefined as any);
            expect(result.isValid).toBe(false);
            expect(result.errors.some(error => error.includes('Asset is required'))).toBe(true);
        });
    });

    describe("transformAssetForWorkspace", () => {
        it("should transform asset with all properties", () => {
            // Act
            const result = assetManager.transformAssetForWorkspace(mockAsset);

            // Assert
            expect(result).toEqual({
                assetData: mockAsset,
                imageUrl: mockAsset.imageUrl,
                naturalDimensions: {
                    width: mockAsset.width,
                    height: mockAsset.height
                }
            });
        });

        it("should handle asset without dimensions", () => {
            // Arrange
            const assetWithoutDimensions = {
                ...mockAsset,
                width: undefined,
                height: undefined
            } as unknown as Asset;

            // Act
            const result = assetManager.transformAssetForWorkspace(assetWithoutDimensions);

            // Assert
            expect(result.naturalDimensions).toBeNull();
            expect(result.assetData).toEqual(assetWithoutDimensions);
        });

        it("should handle asset without imageUrl", () => {
            // Arrange
            const assetWithoutUrl = {
                ...mockAsset,
                imageUrl: undefined
            } as Asset;

            // Act
            const result = assetManager.transformAssetForWorkspace(assetWithoutUrl);

            // Assert
            expect(result.imageUrl).toBeNull();
            expect(result.assetData).toEqual(assetWithoutUrl);
        });

        it("should handle asset with partial dimensions", () => {
            // Arrange
            const assetWithPartialDimensions = {
                ...mockAsset,
                width: 800,
                height: undefined
            } as unknown as Asset;

            // Act
            const result = assetManager.transformAssetForWorkspace(assetWithPartialDimensions);

            // Assert
            expect(result.naturalDimensions).toBeNull();
        });
    });

    describe("getImageAspectRatio", () => {
        it("should calculate aspect ratio for valid dimensions", () => {
            // Act
            const ratio = assetManager.getImageAspectRatio(mockAsset);

            // Assert
            expect(ratio).toBe(800 / 600);
        });

        it("should return null for asset without dimensions", () => {
            // Arrange
            const assetWithoutDimensions = {
                ...mockAsset,
                width: undefined,
                height: undefined
            } as unknown as Asset;

            // Act
            const ratio = assetManager.getImageAspectRatio(assetWithoutDimensions);

            // Assert
            expect(ratio).toBeNull();
        });

        it("should return null for zero height", () => {
            // Arrange
            const assetWithZeroHeight = {
                ...mockAsset,
                height: 0
            };

            // Act
            const ratio = assetManager.getImageAspectRatio(assetWithZeroHeight);

            // Assert
            expect(ratio).toBeNull();
        });
    });

    describe("canLoadAsset", () => {
        it("should allow loading ready asset", () => {
            // Arrange
            const readyAsset = {
                ...mockAsset,
                status: AssetStatus.READY_FOR_ANNOTATION
            };

            // Act
            const canLoad = assetManager.canLoadAsset(readyAsset);

            // Assert
            expect(canLoad).toBe(true);
        });

        it("should prevent loading processing asset", () => {
            // Arrange
            const processingAsset = {
                ...mockAsset,
                status: AssetStatus.PROCESSING
            };

            // Act
            const canLoad = assetManager.canLoadAsset(processingAsset);

            // Assert
            expect(canLoad).toBe(false);
        });

        it("should prevent loading failed asset", () => {
            // Arrange
            const failedAsset = {
                ...mockAsset,
                status: AssetStatus.PROCESSING_ERROR
            };

            // Act
            const canLoad = assetManager.canLoadAsset(failedAsset);

            // Assert
            expect(canLoad).toBe(false);
        });
    });

    describe("Error Handling", () => {
        it("should handle network errors gracefully", async () => {
            // Arrange
            const networkError = new Error("Network timeout");
            vi.mocked(assetService.getAssetById).mockRejectedValue(networkError);

            // Act
            const result = await assetManager.loadAsset("123", "456");

            // Assert
            expect(result.success).toBe(false);
            if (!result.success) {
                expect(result.error).toBe("Network timeout");
            }
        });

        it("should handle unexpected errors", async () => {
            // Arrange
            vi.mocked(assetService.getAssetById).mockRejectedValue("Unexpected error");

            // Act
            const result = await assetManager.loadAsset("123", "456");

            // Assert
            expect(result.success).toBe(false);
            if (!result.success) {
                expect(result.error).toContain("Failed to load asset");
            }
        });
    });

    describe("Performance", () => {
        it("should handle loading multiple assets efficiently", async () => {
            // Arrange
            const assets = Array.from({ length: 5 }, (_, i) => ({
                ...mockAsset,
                id: i + 1
            }));

            vi.mocked(assetService.getAssetById)
                .mockImplementation((_, assetId) => 
                    Promise.resolve(assets.find(a => a.id === assetId) || mockAsset)
                );

            // Act
            const startTime = Date.now();
            const promises = assets.map((_, i) => 
                assetManager.loadAsset("123", String(i + 1))
            );
            const results = await Promise.all(promises);
            const endTime = Date.now();

            // Assert
            expect(results).toHaveLength(5);
            results.forEach(result => {
                expect(result.success).toBe(true);
            });

            // Should complete reasonably quickly (adjust threshold as needed)
            expect(endTime - startTime).toBeLessThan(1000);
        });
    });
});