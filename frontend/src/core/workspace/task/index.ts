export { TaskManager } from './taskManager';
export { TaskNavigationManager } from './taskNavigationManager';

// Re-export types from their proper location
export type { TaskResult, TaskService, PermissionsService } from './taskManager.types';
export type { NavigationResult, NavigationInfo } from './taskNavigationManager.types';
export type { TimerService } from '@/core/timing';
// Logger is used via AppLogger.createServiceLogger() - no type export needed