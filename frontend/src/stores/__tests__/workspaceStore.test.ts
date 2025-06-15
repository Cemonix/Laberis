import { describe, it, expect, beforeEach, vi, afterEach } from "vitest";
import { setActivePinia, createPinia } from "pinia";
import { useWorkspaceStore } from "../workspaceStore";
import { ToolName } from "@/types/workspace/tools";
import type { ImageDimensions } from "@/types/image/imageDimensions";
import type { Point } from "@/types/common/point";
import type { Annotation } from "@/types/workspace/annotation";
import type { LabelScheme } from "@/types/label/labelScheme";

// Mock the annotation service
vi.mock("@/services/api/annotationService", () => ({
    fetchAnnotations: vi.fn(),
    saveAnnotation: vi.fn(),
}));

import {
    fetchAnnotations,
    saveAnnotation,
} from "@/services/api/annotationService";

// Mock the timer utility
vi.mock("@/utils/timer", () => ({
    Timer: vi.fn().mockImplementation(() => ({
        isRunning: false,
        isPaused: false,
        start: vi.fn(),
        pause: vi.fn(),
        stop: vi.fn(),
        reset: vi.fn(),
        getFormattedElapsedTime: vi.fn(() => "00:00:00"),
    })),
}));

// Mock window functions
Object.defineProperty(window, "setInterval", {
    writable: true,
    value: vi.fn((callback) => {
        callback(); // Execute immediately for tests
        return 123; // Mock interval ID
    }),
});

Object.defineProperty(window, "clearInterval", {
    writable: true,
    value: vi.fn(),
});

// Mock alert
Object.defineProperty(window, "alert", {
    writable: true,
    value: vi.fn(),
});

describe("Workspace Store", () => {
    let workspaceStore: ReturnType<typeof useWorkspaceStore>;

    // Mock data
    const mockImageDimensions: ImageDimensions = {
        width: 800,
        height: 600,
    };

    const mockPoint: Point = {
        x: 100,
        y: 150,
    };

    const mockAnnotation: Annotation = {
        annotationId: 1,
        clientId: "client-123",
        annotationType: "point",
        taskId: 1,
        assetId: 1,
        labelId: 1,
        coordinates: { type: "point", point: { x: 100, y: 150 } },
        createdAt: "2024-01-01T00:00:00Z",
        updatedAt: "2024-01-01T00:00:00Z",
    };

    const mockLabelScheme: LabelScheme = {
        labelSchemeId: 2,
        name: "Test Scheme",
        projectId: 1,
        labels: [
            {
                labelId: 10,
                name: "Test Label",
                color: "#FF0000",
                labelSchemeId: 2,
                description: "A test label",
            },
            {
                labelId: 11,
                name: "Another Label",
                color: "#00FF00",
                labelSchemeId: 2,
                description: "Another test label",
            },
        ],
    };

    beforeEach(() => {
        // Create a fresh pinia instance for each test
        setActivePinia(createPinia());
        workspaceStore = useWorkspaceStore();

        // Reset all mocks
        vi.clearAllMocks();
    });

    afterEach(() => {
        // Clean up any intervals
        if (workspaceStore.timerIntervalId) {
            clearInterval(workspaceStore.timerIntervalId);
        }
    });

    describe("Initial State", () => {
        it("should have correct initial state", () => {
            expect(workspaceStore.currentProjectId).toBeNull();
            expect(workspaceStore.currentAssetId).toBeNull();
            expect(workspaceStore.currentImageUrl).toBeNull();
            expect(workspaceStore.imageNaturalDimensions).toBeNull();
            expect(workspaceStore.canvasDisplayDimensions).toBeNull();
            expect(workspaceStore.elapsedTimeDisplay).toBe("00:00:00");
            expect(workspaceStore.timerIntervalId).toBeNull();
            expect(workspaceStore.viewOffset).toEqual({ x: 0, y: 0 });
            expect(workspaceStore.zoomLevel).toBe(1.0);
            expect(workspaceStore.activeTool).toBe(ToolName.CURSOR);
            expect(workspaceStore.annotations).toEqual([]);
            expect(workspaceStore.currentLabelId).toBeNull();
            expect(workspaceStore.currentTaskId).toBeNull();
        });

        it("should have default available tools", () => {
            expect(workspaceStore.availableTools).toHaveLength(6);
            expect(
                workspaceStore.availableTools.map((tool) => tool.id)
            ).toEqual([
                ToolName.CURSOR,
                ToolName.POINT,
                ToolName.BOUNDING_BOX,
                ToolName.LINE,
                ToolName.POLYLINE,
                ToolName.POLYGON,
            ]);
        });

        it("should have timer instance", () => {
            // Timer instance should be defined and have the expected interface
            expect(workspaceStore.timerInstance).toBeDefined();
            expect(workspaceStore.timerInstance).toHaveProperty("isRunning");
            expect(workspaceStore.timerInstance).toHaveProperty("start");
            expect(workspaceStore.timerInstance).toHaveProperty("pause");
            expect(workspaceStore.timerInstance).toHaveProperty("stop");
            expect(workspaceStore.timerInstance).toHaveProperty("reset");
            expect(workspaceStore.timerInstance).toHaveProperty(
                "getFormattedElapsedTime"
            );
        });
    });

    describe("Getters", () => {
        it("should calculate current image aspect ratio", () => {
            expect(workspaceStore.getCurrentImageAspectRatio).toBeNull();

            workspaceStore.setImageNaturalDimensions(mockImageDimensions);
            expect(workspaceStore.getCurrentImageAspectRatio).toBe(800 / 600);
        });

        it("should return zoom config", () => {
            const zoomConfig = workspaceStore.getZoomConfig;
            expect(zoomConfig.minZoom).toBe(0.1);
            expect(zoomConfig.maxZoom).toBe(10.0);
            expect(zoomConfig.zoomSensitivity).toBe(0.005);
        });

        it("should get active tool details", () => {
            expect(workspaceStore.getActiveToolDetails?.id).toBe(
                ToolName.CURSOR
            );
            expect(workspaceStore.getActiveToolDetails?.name).toBe("Cursor");

            workspaceStore.setActiveTool(ToolName.POINT);
            expect(workspaceStore.getActiveToolDetails?.id).toBe(
                ToolName.POINT
            );
            expect(workspaceStore.getActiveToolDetails?.name).toBe("Point");
        });

        it("should get annotations", () => {
            expect(workspaceStore.getAnnotations).toEqual([]);

            workspaceStore.setAnnotations([mockAnnotation]);
            expect(workspaceStore.getAnnotations).toEqual([mockAnnotation]);
        });

        it("should get selected label id", () => {
            expect(workspaceStore.getSelectedLabelId).toBeNull();

            workspaceStore.setCurrentLabelId(5);
            expect(workspaceStore.getSelectedLabelId).toBe(5);
        });

        it("should get current label scheme", () => {
            // Store starts with a default label scheme
            expect(workspaceStore.getCurrentLabelScheme).not.toBeNull();
            expect(workspaceStore.getCurrentLabelScheme?.name).toBe(
                "Default Scheme"
            );

            workspaceStore.setCurrentLabelScheme(mockLabelScheme);
            expect(workspaceStore.getCurrentLabelScheme).toEqual(
                mockLabelScheme
            );

            workspaceStore.setCurrentLabelScheme(null);
            expect(workspaceStore.getCurrentLabelScheme).toBeNull();
        });

        it("should get label by id", () => {
            workspaceStore.setCurrentLabelScheme(mockLabelScheme);

            const label = workspaceStore.getLabelById(10);
            expect(label?.name).toBe("Test Label");
            expect(label?.color).toBe("#FF0000");

            const nonExistentLabel = workspaceStore.getLabelById(999);
            expect(nonExistentLabel).toBeUndefined();
        });

        it("should return undefined when getting label by id without scheme", () => {
            const label = workspaceStore.getLabelById(10);
            expect(label).toBeUndefined();
        });
    });

    describe("Asset Loading", () => {
        it("should load asset successfully", async () => {
            vi.mocked(fetchAnnotations).mockResolvedValue([mockAnnotation]);

            await workspaceStore.loadAsset("project-1", "asset-1");

            expect(workspaceStore.currentProjectId).toBe("project-1");
            expect(workspaceStore.currentAssetId).toBe("asset-1");
            expect(workspaceStore.currentImageUrl).toMatch(
                /https:\/\/picsum\.photos\/800\/600\?random=/
            );
            expect(workspaceStore.imageNaturalDimensions).toBeNull();
            expect(workspaceStore.annotations).toEqual([mockAnnotation]);
            expect(workspaceStore.currentLabelId).toBeNull();
            expect(workspaceStore.activeTool).toBe(ToolName.CURSOR);

            // Check that timer was started
            expect(workspaceStore.timerInstance.start).toHaveBeenCalled();
            expect(window.setInterval).toHaveBeenCalled();
        });

        it("should handle fetch annotations error during asset loading", async () => {
            const consoleError = vi
                .spyOn(console, "error")
                .mockImplementation(() => {});
            vi.mocked(fetchAnnotations).mockRejectedValue(
                new Error("Network error")
            );

            await workspaceStore.loadAsset("project-1", "asset-1");

            expect(workspaceStore.annotations).toEqual([]);
            expect(consoleError).toHaveBeenCalledWith(
                "Failed to fetch annotations:",
                expect.any(Error)
            );

            consoleError.mockRestore();
        });
    });

    describe("Image Dimensions", () => {
        it("should set image natural dimensions", () => {
            const consoleSpy = vi
                .spyOn(console, "log")
                .mockImplementation(() => {});

            workspaceStore.setImageNaturalDimensions(mockImageDimensions);

            expect(workspaceStore.imageNaturalDimensions).toEqual(
                mockImageDimensions
            );
            expect(consoleSpy).toHaveBeenCalledWith(
                "[Store] Set image natural dimensions:",
                mockImageDimensions
            );

            consoleSpy.mockRestore();
        });

        it("should set canvas display dimensions", () => {
            const consoleSpy = vi
                .spyOn(console, "log")
                .mockImplementation(() => {});

            workspaceStore.setCanvasDisplayDimensions(mockImageDimensions);

            expect(workspaceStore.canvasDisplayDimensions).toEqual(
                mockImageDimensions
            );
            expect(consoleSpy).toHaveBeenCalledWith(
                "[Store] Set canvas display dimensions:",
                mockImageDimensions
            );

            consoleSpy.mockRestore();
        });
    });

    describe("Annotations Management", () => {
        it("should set annotations", () => {
            const annotations = [mockAnnotation];
            workspaceStore.setAnnotations(annotations);

            expect(workspaceStore.annotations).toEqual(annotations);
        });

        it("should add annotation successfully", async () => {
            const savedAnnotation = { ...mockAnnotation, annotationId: 2 };
            vi.mocked(saveAnnotation).mockResolvedValue(savedAnnotation);

            await workspaceStore.addAnnotation(mockAnnotation);

            expect(workspaceStore.annotations).toHaveLength(1);
            expect(saveAnnotation).toHaveBeenCalledWith(mockAnnotation);
            expect(workspaceStore.annotations[0]).toEqual(savedAnnotation);
        });

        it("should handle annotation save failure", async () => {
            const consoleError = vi
                .spyOn(console, "error")
                .mockImplementation(() => {});
            vi.mocked(saveAnnotation).mockRejectedValue(
                new Error("Save failed")
            );

            await workspaceStore.addAnnotation(mockAnnotation);

            expect(workspaceStore.annotations).toEqual([]);
            expect(consoleError).toHaveBeenCalledWith(
                "Failed to save annotation:",
                expect.any(Error)
            );
            expect(window.alert).toHaveBeenCalledWith(
                "Could not save annotation. Please try again."
            );

            consoleError.mockRestore();
        });
    });

    describe("Label Management", () => {
        it("should set current label id", () => {
            const consoleSpy = vi
                .spyOn(console, "log")
                .mockImplementation(() => {});

            workspaceStore.setCurrentLabelId(5);
            expect(workspaceStore.currentLabelId).toBe(5);
            expect(consoleSpy).toHaveBeenCalledWith(
                "[Store] Selected Label ID: 5 (Unknown)"
            );

            workspaceStore.setCurrentLabelId(null);
            expect(workspaceStore.currentLabelId).toBeNull();
            expect(consoleSpy).toHaveBeenCalledWith(
                "[Store] Label selection cleared."
            );

            consoleSpy.mockRestore();
        });

        it("should set current label id with known label name", () => {
            const consoleSpy = vi
                .spyOn(console, "log")
                .mockImplementation(() => {});
            workspaceStore.setCurrentLabelScheme(mockLabelScheme);

            workspaceStore.setCurrentLabelId(10);
            expect(consoleSpy).toHaveBeenCalledWith(
                "[Store] Selected Label ID: 10 (Test Label)"
            );

            consoleSpy.mockRestore();
        });

        it("should set current label scheme", () => {
            const consoleSpy = vi
                .spyOn(console, "log")
                .mockImplementation(() => {});

            workspaceStore.setCurrentLabelScheme(mockLabelScheme);
            expect(workspaceStore.currentLabelScheme).toEqual(mockLabelScheme);
            expect(consoleSpy).toHaveBeenCalledWith(
                "[Store] Label Scheme set:",
                "Test Scheme"
            );

            workspaceStore.setCurrentLabelScheme(null);
            expect(workspaceStore.currentLabelScheme).toBeNull();
            expect(consoleSpy).toHaveBeenCalledWith(
                "[Store] Label Scheme cleared."
            );

            consoleSpy.mockRestore();
        });
    });

    describe("Task Management", () => {
        it("should set current task id", () => {
            const consoleSpy = vi
                .spyOn(console, "log")
                .mockImplementation(() => {});

            workspaceStore.setCurrentTaskId(123);
            expect(workspaceStore.currentTaskId).toBe(123);
            expect(consoleSpy).toHaveBeenCalledWith(
                "[Store] Current Task ID set:",
                123
            );

            workspaceStore.setCurrentTaskId(null);
            expect(workspaceStore.currentTaskId).toBeNull();
            expect(consoleSpy).toHaveBeenCalledWith(
                "[Store] Current Task ID cleared."
            );

            consoleSpy.mockRestore();
        });
    });

    describe("Timer Management", () => {
        it("should start timer", () => {
            workspaceStore.startTimer();

            expect(workspaceStore.timerInstance.start).toHaveBeenCalled();
            expect(window.setInterval).toHaveBeenCalled();
            expect(workspaceStore.timerIntervalId).toBe(123);
        });

        it("should pause timer when running", () => {
            workspaceStore.timerInstance.isRunning = true;
            workspaceStore.timerIntervalId = 123;

            workspaceStore.pauseTimer();

            expect(workspaceStore.timerInstance.pause).toHaveBeenCalled();
            expect(window.clearInterval).toHaveBeenCalledWith(123);
        });

        it("should not pause timer when not running", () => {
            workspaceStore.timerInstance.isRunning = false;

            workspaceStore.pauseTimer();

            expect(workspaceStore.timerInstance.pause).not.toHaveBeenCalled();
        });

        it("should stop and reset timer", () => {
            workspaceStore.timerIntervalId = 123;

            workspaceStore.stopAndResetTimer();

            expect(workspaceStore.timerInstance.stop).toHaveBeenCalled();
            expect(workspaceStore.timerInstance.reset).toHaveBeenCalled();
            expect(window.clearInterval).toHaveBeenCalledWith(123);
            expect(workspaceStore.elapsedTimeDisplay).toBe("00:00:00");
        });

        it("should cleanup timer", () => {
            workspaceStore.timerIntervalId = 123;

            workspaceStore.cleanupTimer();

            expect(window.clearInterval).toHaveBeenCalledWith(123);
            expect(workspaceStore.timerInstance.stop).toHaveBeenCalled();
        });
    });

    describe("View and Zoom Management", () => {
        it("should set view offset", () => {
            workspaceStore.setViewOffset(mockPoint);
            expect(workspaceStore.viewOffset).toEqual(mockPoint);
        });

        it("should set zoom level within bounds", () => {
            workspaceStore.setZoomLevel(2.5);
            expect(workspaceStore.zoomLevel).toBe(2.5);

            // Test minimum bound
            workspaceStore.setZoomLevel(0.05);
            expect(workspaceStore.zoomLevel).toBe(0.1);

            // Test maximum bound
            workspaceStore.setZoomLevel(15);
            expect(workspaceStore.zoomLevel).toBe(10.0);
        });
    });

    describe("Tool Management", () => {
        it("should set active tool for valid tool", () => {
            workspaceStore.setActiveTool(ToolName.POINT);
            expect(workspaceStore.activeTool).toBe(ToolName.POINT);

            workspaceStore.setActiveTool(ToolName.POLYGON);
            expect(workspaceStore.activeTool).toBe(ToolName.POLYGON);
        });

        it("should not set active tool for invalid tool", () => {
            const originalTool = workspaceStore.activeTool;

            // Try to set an invalid tool (cast to bypass TypeScript checking)
            workspaceStore.setActiveTool("INVALID_TOOL" as ToolName);

            expect(workspaceStore.activeTool).toBe(originalTool);
        });
    });

    describe("Private Methods", () => {
        it("should clear interval when timer interval id exists", () => {
            workspaceStore.timerIntervalId = 123;

            workspaceStore._clearInterval();

            expect(window.clearInterval).toHaveBeenCalledWith(123);
            expect(workspaceStore.timerIntervalId).toBeNull();
        });

        it("should not clear interval when timer interval id is null", () => {
            workspaceStore.timerIntervalId = null;

            workspaceStore._clearInterval();

            expect(window.clearInterval).not.toHaveBeenCalled();
        });

        it("should update elapsed time display", () => {
            vi.mocked(
                workspaceStore.timerInstance.getFormattedElapsedTime
            ).mockReturnValue("01:23:45");

            workspaceStore._updateElapsedTimeDisplay();

            expect(workspaceStore.elapsedTimeDisplay).toBe("01:23:45");
        });
    });

    describe("Default Label Scheme", () => {
        it("should have default label scheme with sample labels", () => {
            expect(workspaceStore.currentLabelScheme).toBeDefined();
            expect(workspaceStore.currentLabelScheme?.name).toBe(
                "Default Scheme"
            );
            expect(workspaceStore.currentLabelScheme?.labels).toHaveLength(3);
            expect(
                workspaceStore.currentLabelScheme?.labels.map((l) => l.name)
            ).toEqual(["Person", "Car", "Tree"]);
        });
    });
});
