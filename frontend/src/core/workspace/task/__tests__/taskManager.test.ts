import { describe, it, expect, beforeEach, vi } from 'vitest';
import { TaskManager } from '../taskManager';
import { TaskNavigationManager } from '../taskNavigationManager';
import type { Task } from '@/services/project/task/task.types';
import { TaskStatus } from '@/services/project/task/task.types';
import type { PipelineResultDto } from '@/services/project/task/task.types';

// Mock logger
const mockLogger = {
    info: vi.fn(),
    error: vi.fn(),
    warn: vi.fn(),
    debug: vi.fn()
};

// Mock task service
const mockTaskService = {
    getTaskById: vi.fn(),
    getTasksForAsset: vi.fn(),
    getTasksForStage: vi.fn(),
    completeTaskPipeline: vi.fn(),
    vetoTaskPipeline: vi.fn(),
    changeTaskStatus: vi.fn(),
    updateWorkingTime: vi.fn(),
    saveWorkingTimeBeforeUnload: vi.fn()
};

// Mock permissions
const mockPermissions = {
    canUpdateProject: vi.fn()
};

// Mock timer
const mockTimer = {
    getElapsedTime: vi.fn(() => 1000),
    isRunning: vi.fn(() => true)
};

// Test data
const mockTask: Task = {
    id: 1,
    priority: 1,
    workingTimeMs: 5000,
    createdAt: '2024-01-01T00:00:00Z',
    updatedAt: '2024-01-01T00:00:00Z',
    assetId: 1,
    projectId: 1,
    workflowId: 1,
    workflowStageId: 1,
    status: TaskStatus.IN_PROGRESS,
    assignedToEmail: 'test@example.com'
};

const mockCompletedTask: Task = {
    ...mockTask,
    status: TaskStatus.COMPLETED,
    completedAt: '2024-01-01T01:00:00Z'
};

const mockSuspendedTask: Task = {
    ...mockTask,
    status: TaskStatus.SUSPENDED,
    suspendedAt: '2024-01-01T01:00:00Z'
};

const mockDeferredTask: Task = {
    ...mockTask,
    status: TaskStatus.DEFERRED,
    deferredAt: '2024-01-01T01:00:00Z'
};

const mockPipelineSuccess: PipelineResultDto = {
    isSuccess: true,
    updatedTask: mockCompletedTask,
    details: 'Task completed successfully'
};

const mockPipelineFailure: PipelineResultDto = {
    isSuccess: false,
    errorMessage: 'Pipeline failed'
};

describe('TaskManager', () => {
    let taskManager: TaskManager;

    beforeEach(() => {
        vi.clearAllMocks();
        taskManager = new TaskManager(
            mockTaskService as any,
            mockPermissions as any,
            mockTimer as any,
        );
    });

    describe('Task Completion', () => {
        it('should complete a task successfully using pipeline system', async () => {
            mockTaskService.completeTaskPipeline.mockResolvedValue(mockPipelineSuccess);
            mockTaskService.getTaskById.mockResolvedValue(mockCompletedTask);

            const result = await taskManager.completeTask(123, 1);

            expect(result.success).toBe(true);
            expect(result.task).toEqual(mockCompletedTask);
            expect(result.message).toContain('successfully completed');
            expect(mockTaskService.completeTaskPipeline).toHaveBeenCalledWith(123, 1);
            expect(mockTaskService.getTaskById).toHaveBeenCalledWith(123, 1);
        });

        it('should handle task completion failure', async () => {
            mockTaskService.completeTaskPipeline.mockResolvedValue(mockPipelineFailure);

            const result = await taskManager.completeTask(123, 1);

            expect(result.success).toBe(false);
            expect(result.error).toBe('Pipeline failed');
        });

        it('should handle task completion with service error', async () => {
            mockTaskService.completeTaskPipeline.mockRejectedValue(new Error('Service error'));

            const result = await taskManager.completeTask(123, 1);

            expect(result.success).toBe(false);
            expect(result.error).toBe('Service error');
        });
    });

    describe('Task Suspension', () => {
        it('should suspend a task successfully with working time preservation', async () => {
            mockTaskService.changeTaskStatus.mockResolvedValue(mockSuspendedTask);

            const result = await taskManager.suspendTask(123, 1, 2000);

            expect(result.success).toBe(true);
            expect(result.task).toEqual(mockSuspendedTask);
            expect(mockTaskService.changeTaskStatus).toHaveBeenCalledWith(
                123, 1, { targetStatus: TaskStatus.SUSPENDED }
            );
        });

        it('should handle task suspension failure', async () => {
            mockTaskService.changeTaskStatus.mockRejectedValue(new Error('Suspension failed'));

            const result = await taskManager.suspendTask(123, 1, 2000);

            expect(result.success).toBe(false);
            expect(result.error).toBe('Suspension failed');
        });

        it('should properly preserve working time during suspension', async () => {
            const currentWorkingTime = 5000;
            const elapsedTime = 3000;
            const expectedTotalTime = currentWorkingTime + elapsedTime;
            
            mockTimer.getElapsedTime.mockReturnValue(elapsedTime);
            mockTaskService.changeTaskStatus.mockResolvedValue({
                ...mockSuspendedTask,
                workingTimeMs: expectedTotalTime
            });

            const result = await taskManager.suspendTask(123, 1, currentWorkingTime);

            expect(result.success).toBe(true);
            // The manager should calculate total time and pass it through
            expect(mockTaskService.changeTaskStatus).toHaveBeenCalledWith(
                123, 1, { targetStatus: TaskStatus.SUSPENDED }
            );
        });
    });

    describe('Task Deferring', () => {
        it('should defer a task successfully', async () => {
            mockTaskService.changeTaskStatus.mockResolvedValue(mockDeferredTask);

            const result = await taskManager.deferTask(123, 1, 2000);

            expect(result.success).toBe(true);
            expect(result.task).toEqual(mockDeferredTask);
            expect(mockTaskService.changeTaskStatus).toHaveBeenCalledWith(
                123, 1, { targetStatus: TaskStatus.DEFERRED }
            );
        });

        it('should handle task deferring failure', async () => {
            mockTaskService.changeTaskStatus.mockRejectedValue(new Error('Defer failed'));

            const result = await taskManager.deferTask(123, 1, 2000);

            expect(result.success).toBe(false);
            expect(result.error).toBe('Defer failed');
        });
    });

    describe('Task Veto', () => {
        it('should veto a task successfully with reason', async () => {
            const vetoResult = { ...mockPipelineSuccess, updatedTask: { ...mockTask, status: TaskStatus.VETOED } };
            mockTaskService.vetoTaskPipeline.mockResolvedValue(vetoResult);

            const result = await taskManager.vetoTask(123, 1, 'Quality issues');

            expect(result.success).toBe(true);
            expect(result.task?.status).toBe(TaskStatus.VETOED);
            expect(mockTaskService.vetoTaskPipeline).toHaveBeenCalledWith(123, 1, {
                reason: 'Quality issues'
            });
        });

        it('should handle task veto failure', async () => {
            mockTaskService.vetoTaskPipeline.mockResolvedValue(mockPipelineFailure);

            const result = await taskManager.vetoTask(123, 1, 'Quality issues');

            expect(result.success).toBe(false);
            expect(result.error).toBe('Pipeline failed');
        });

        it('should use default reason when none provided', async () => {
            const vetoResult = { ...mockPipelineSuccess, updatedTask: { ...mockTask, status: TaskStatus.VETOED } };
            mockTaskService.vetoTaskPipeline.mockResolvedValue(vetoResult);

            await taskManager.vetoTask(123, 1);

            expect(mockTaskService.vetoTaskPipeline).toHaveBeenCalledWith(123, 1, {
                reason: 'Task returned for rework'
            });
        });
    });

    describe('Task Validation', () => {
        it('should correctly identify if task can be completed', () => {
            const inProgressTask = { ...mockTask, status: TaskStatus.IN_PROGRESS };
            const completedTask = { ...mockTask, status: TaskStatus.COMPLETED };
            const suspendedTask = { ...mockTask, status: TaskStatus.SUSPENDED };

            expect(taskManager.canCompleteTask(inProgressTask)).toBe(true);
            expect(taskManager.canCompleteTask(completedTask)).toBe(false);
            expect(taskManager.canCompleteTask(suspendedTask)).toBe(false);
        });

        it('should validate task permissions correctly for regular tasks', async () => {
            const result = await taskManager.canOpenTask(mockTask);
            
            expect(result).toBe(true);
            // Regular tasks don't require permission checks
            expect(mockPermissions.canUpdateProject).not.toHaveBeenCalled();
        });

        it('should handle deferred tasks requiring manager permissions', async () => {
            const deferredTask = { ...mockTask, status: TaskStatus.DEFERRED };
            mockPermissions.canUpdateProject.mockResolvedValue(false);

            const result = await taskManager.canOpenTask(deferredTask);
            
            expect(result).toBe(false);
        });

        it('should allow managers to open deferred tasks', async () => {
            const deferredTask = { ...mockTask, status: TaskStatus.DEFERRED };
            mockPermissions.canUpdateProject.mockResolvedValue(true);

            const result = await taskManager.canOpenTask(deferredTask);
            
            expect(result).toBe(true);
        });
    });

    describe('Working Time Management', () => {
        it('should calculate total working time correctly', () => {
            const currentTime = 5000;
            const elapsedTime = 3000;
            mockTimer.getElapsedTime.mockReturnValue(elapsedTime);

            const totalTime = taskManager.calculateTotalWorkingTime(currentTime);

            expect(totalTime).toBe(8000);
            expect(mockTimer.getElapsedTime).toHaveBeenCalled();
        });

        it('should return current time if timer is not running', () => {
            const currentTime = 5000;
            mockTimer.isRunning.mockReturnValue(false);
            mockTimer.getElapsedTime.mockReturnValue(0);

            const totalTime = taskManager.calculateTotalWorkingTime(currentTime);

            expect(totalTime).toBe(5000);
        });
    });

    describe('Error Handling', () => {
        it('should log appropriate errors for each operation', async () => {
            mockTaskService.completeTaskPipeline.mockRejectedValue(new Error('Test error'));

            await taskManager.completeTask(123, 1);

            expect(mockLogger.error).toHaveBeenCalledWith(
                'Failed to complete task via pipeline:',
                expect.any(Error)
            );
        });

        it('should handle non-Error exceptions', async () => {
            mockTaskService.completeTaskPipeline.mockRejectedValue('String error');

            const result = await taskManager.completeTask(123, 1);

            expect(result.success).toBe(false);
            expect(result.error).toBe('Failed to complete task');
        });
    });
});

describe('TaskNavigationManager', () => {
    let navigationManager: TaskNavigationManager;
    
    const mockTasks: Task[] = [
        { ...mockTask, id: 1, status: TaskStatus.COMPLETED },
        { ...mockTask, id: 2, status: TaskStatus.IN_PROGRESS },
        { ...mockTask, id: 3, status: TaskStatus.NOT_STARTED },
        { ...mockTask, id: 4, status: TaskStatus.COMPLETED }
    ];

    beforeEach(() => {
        vi.clearAllMocks();
        navigationManager = new TaskNavigationManager(
            mockPermissions as any,
        );
    });

    describe('Next Task Navigation', () => {
        it('should find next available task', async () => {
            mockPermissions.canUpdateProject.mockResolvedValue(true);
            
            const result = await navigationManager.navigateToNext(
                mockTasks[1], // current task (id: 2)
                mockTasks,
                '123'
            );

            expect(result.success).toBe(true);
            expect(result.navigation).toEqual({
                projectId: '123',
                assetId: '1',
                taskId: '3'
            });
        });

        it('should return null when no next task available', async () => {
            mockPermissions.canUpdateProject.mockResolvedValue(true);
            
            const result = await navigationManager.navigateToNext(
                mockTasks[3], // current task (id: 4) - last task
                mockTasks,
                '123'
            );

            expect(result.success).toBe(true);
            expect(result.navigation).toBeNull();
        });

        it('should skip tasks that cannot be opened', async () => {
            // Mock deferred task that requires manager permissions
            const tasksWithDeferred = [
                ...mockTasks,
                { ...mockTask, id: 5, status: TaskStatus.DEFERRED }
            ];
            
            // User is not a manager
            mockPermissions.canUpdateProject.mockResolvedValue(false);
            
            const result = await navigationManager.navigateToNext(
                mockTasks[1],
                tasksWithDeferred,
                '123'
            );

            expect(result.success).toBe(true);
            // Should skip deferred task and go to id: 3
            expect(result.navigation?.taskId).toBe('3');
        });
    });

    describe('Previous Task Navigation', () => {
        it('should find previous available task', async () => {
            mockPermissions.canUpdateProject.mockResolvedValue(true);
            
            const result = await navigationManager.navigateToPrevious(
                mockTasks[2], // current task (id: 3)
                mockTasks,
                '123'
            );

            expect(result.success).toBe(true);
            expect(result.navigation).toEqual({
                projectId: '123',
                assetId: '1',
                taskId: '2'
            });
        });

        it('should return null when no previous task available', async () => {
            mockPermissions.canUpdateProject.mockResolvedValue(true);
            
            const result = await navigationManager.navigateToPrevious(
                mockTasks[0], // first task
                mockTasks,
                '123'
            );

            expect(result.success).toBe(true);
            expect(result.navigation).toBeNull();
        });
    });

    describe('Next Available Task (Completion Helper)', () => {
        it('should find next uncompleted task', async () => {
            const result = await navigationManager.getNextAvailableTask(
                mockTasks[1], // current task (id: 2)
                mockTasks,
                '123'
            );

            expect(result.success).toBe(true);
            expect(result.navigation).toEqual({
                projectId: '123',
                assetId: '1',
                taskId: '3'
            });
        });

        it('should wrap around to beginning if needed', async () => {
            const tasksWithGap = [
                { ...mockTask, id: 1, status: TaskStatus.NOT_STARTED },
                { ...mockTask, id: 2, status: TaskStatus.COMPLETED },
                { ...mockTask, id: 3, status: TaskStatus.COMPLETED }
            ];
            
            const result = await navigationManager.getNextAvailableTask(
                tasksWithGap[2], // last task
                tasksWithGap,
                '123'
            );

            expect(result.success).toBe(true);
            expect(result.navigation?.taskId).toBe('1');
        });

        it('should return null when all tasks are completed', async () => {
            const completedTasks = mockTasks.map(task => ({
                ...task,
                status: TaskStatus.COMPLETED,
                completedAt: '2024-01-01T01:00:00Z'
            }));
            
            const result = await navigationManager.getNextAvailableTask(
                completedTasks[1],
                completedTasks,
                '123'
            );

            expect(result.success).toBe(true);
            expect(result.navigation).toBeNull();
        });
    });

    describe('Navigation Info', () => {
        it('should provide correct navigation information', () => {
            const info = navigationManager.getNavigationInfo(
                mockTasks[1],
                mockTasks
            );

            expect(info.currentIndex).toBe(1);
            expect(info.totalTasks).toBe(4);
            expect(info.hasNext).toBe(true);
            expect(info.hasPrevious).toBe(true);
        });

        it('should handle edge cases correctly', () => {
            // First task
            const firstInfo = navigationManager.getNavigationInfo(
                mockTasks[0],
                mockTasks
            );
            expect(firstInfo.currentIndex).toBe(0);
            expect(firstInfo.hasPrevious).toBe(false);
            expect(firstInfo.hasNext).toBe(true);

            // Last task
            const lastInfo = navigationManager.getNavigationInfo(
                mockTasks[mockTasks.length - 1],
                mockTasks
            );
            expect(lastInfo.currentIndex).toBe(3);
            expect(lastInfo.hasPrevious).toBe(true);
            expect(lastInfo.hasNext).toBe(false);
        });

        it('should handle single task scenario', () => {
            const singleTask = [mockTasks[0]];
            const info = navigationManager.getNavigationInfo(
                singleTask[0],
                singleTask
            );

            expect(info.currentIndex).toBe(0);
            expect(info.totalTasks).toBe(1);
            expect(info.hasNext).toBe(false);
            expect(info.hasPrevious).toBe(false);
        });
    });

    describe('Error Handling', () => {
        it('should handle empty task list', async () => {
            const result = await navigationManager.navigateToNext(
                mockTask,
                [],
                '123'
            );

            expect(result.success).toBe(true);
            expect(result.navigation).toBeNull();
        });

        it('should handle missing current task', async () => {
            const result = await navigationManager.navigateToNext(
                { ...mockTask, id: 999 }, // not in the list
                mockTasks,
                '123'
            );

            expect(result.success).toBe(true);
            expect(result.navigation).toBeNull();
        });
    });
});