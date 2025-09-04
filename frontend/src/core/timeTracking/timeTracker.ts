import { AppLogger } from '@/core/logger/logger';
import type { TimerService } from './types';

interface TimeTrackingSession {
    taskId: number;
    startTime: number;
    lastPauseTime?: number;
    accumulatedTime: number;
    isActive: boolean;
}

interface PersistentTimeData {
    [taskId: string]: {
        totalTime: number;
        sessionStartTime: number;
        lastSyncTime: number;
        pendingTime: number;
    };
}

export class TimeTracker implements TimerService {
    private logger = AppLogger.createServiceLogger('TimeTracker');
    private currentSession: TimeTrackingSession | null = null;
    private syncInterval: number | null = null;
    private localStorageKey = 'laberis-time-tracking';
    private isVisible = true;
    private syncCallback: ((taskId: number, totalTimeMs: number) => Promise<void>) | null = null;
    
    // Store references to event handlers for proper cleanup
    private handleVisibilityChange = () => {
        const wasVisible = this.isVisible;
        this.isVisible = !document.hidden;

        if (wasVisible && !this.isVisible) {
            this.pauseTracking();
            this.logger.debug('Tab hidden - paused tracking');
        } else if (!wasVisible && this.isVisible) {
            this.resumeTracking();
            this.logger.debug('Tab visible - resumed tracking');
        }
    };

    private handleFocus = () => {
        if (!this.isVisible) {
            this.isVisible = true;
            this.resumeTracking();
        }
    };

    private handleBlur = () => {
        if (this.isVisible) {
            this.isVisible = false;
            this.pauseTracking();
        }
    };

    private handleBeforeUnload = () => {
        this.saveForUnload();
    };

    private handlePageHide = () => {
        this.saveForUnload();
    };

    private handleUnload = () => {
        this.saveForUnload();
    };

    constructor() {
        this.setupVisibilityHandlers();
        this.setupUnloadHandlers();
        this.startPeriodicSync();
    }

    /**
     * Start tracking time for a task
     */
    startTracking(taskId: number, previousWorkingTimeMs: number = 0, syncCallback?: (taskId: number, totalTimeMs: number) => Promise<void>) {
        this.logger.info(`Starting time tracking for task ${taskId}, previous time: ${previousWorkingTimeMs}ms`);
        
        // Save any existing session before starting new one
        if (this.currentSession && this.currentSession.taskId !== taskId) {
            this.pauseTracking();
            this.saveSessionToStorage();
        }

        this.syncCallback = syncCallback || null;
        const now = Date.now();
        
        // Check if we have stored data for this task
        const storedData = this.getStoredTimeData();
        const existingData = storedData[taskId.toString()];
        
        let accumulatedTime = previousWorkingTimeMs;
        
        // If we have local storage data that's more recent than the server data, use it
        if (existingData && existingData.totalTime > previousWorkingTimeMs) {
            accumulatedTime = existingData.totalTime;
            this.logger.info(`Using stored time data: ${accumulatedTime}ms (server had ${previousWorkingTimeMs}ms)`);
        }

        this.currentSession = {
            taskId,
            startTime: now,
            accumulatedTime,
            isActive: this.isVisible
        };

        // Update stored data
        this.updateStoredTimeData(taskId, {
            totalTime: accumulatedTime,
            sessionStartTime: now,
            lastSyncTime: now,
            pendingTime: 0
        });

        this.logger.info(`Started tracking task ${taskId} with accumulated time: ${accumulatedTime}ms`);
    }

    /**
     * Pause tracking (when tab becomes hidden or user becomes inactive)
     */
    pauseTracking() {
        if (!this.currentSession || !this.currentSession.isActive) {
            return;
        }

        const now = Date.now();
        const sessionTime = now - this.currentSession.startTime;
        this.currentSession.accumulatedTime += sessionTime;
        this.currentSession.lastPauseTime = now;
        this.currentSession.isActive = false;

        this.logger.debug(`Paused tracking, added ${sessionTime}ms, total: ${this.currentSession.accumulatedTime}ms`);
        this.saveSessionToStorage();
    }

    /**
     * Resume tracking (when tab becomes visible again)
     */
    resumeTracking() {
        if (!this.currentSession || this.currentSession.isActive) {
            return;
        }

        const now = Date.now();
        this.currentSession.startTime = now;
        this.currentSession.isActive = true;
        delete this.currentSession.lastPauseTime;

        this.logger.debug(`Resumed tracking at ${now}`);
    }

    /**
     * Stop tracking and return total time
     */
    stopTracking(): number {
        if (!this.currentSession) {
            return 0;
        }

        if (this.currentSession.isActive) {
            this.pauseTracking();
        }

        const totalTime = this.currentSession.accumulatedTime;
        const taskId = this.currentSession.taskId;

        // Clear the session
        this.currentSession = null;

        // Clean up stored data for this task
        this.clearStoredTimeData(taskId);

        this.logger.info(`Stopped tracking, total time: ${totalTime}ms`);
        return totalTime;
    }

    /**
     * Get current total time (including current session)
     */
    getCurrentTime(): number {
        if (!this.currentSession) {
            return 0;
        }

        let totalTime = this.currentSession.accumulatedTime;
        
        if (this.currentSession.isActive) {
            const currentSessionTime = Date.now() - this.currentSession.startTime;
            totalTime += currentSessionTime;
        }

        return totalTime;
    }

    // TimerService interface methods
    
    /**
     * Check if timer is currently running
     */
    isRunning(): boolean {
        return !!(this.currentSession && this.currentSession.isActive);
    }

    /**
     * Get elapsed time (alias for getCurrentTime for TimerService compatibility)
     */
    getElapsedTime(): number {
        return this.getCurrentTime();
    }

    /**
     * Start timer (simplified interface for TaskManager compatibility)
     */
    start(): void {
        if (this.currentSession) {
            this.resumeTracking();
        }
        // Note: For full functionality, use startTracking() with taskId
    }

    /**
     * Stop timer (alias for stopTracking)
     */
    stop(): void {
        this.stopTracking();
    }

    /**
     * Pause timer
     */
    pause(): void {
        this.pauseTracking();
    }

    /**
     * Resume timer
     */
    resume(): void {
        this.resumeTracking();
    }

    /**
     * Reset timer
     */
    reset(): void {
        if (this.currentSession) {
            this.currentSession.accumulatedTime = 0;
            this.currentSession.startTime = Date.now();
            this.saveSessionToStorage();
        }
    }

    /**
     * Format time in milliseconds to HH:MM:SS string
     */
    getFormattedElapsedTime(durationMs?: number): string {
        const timeToFormat = durationMs !== undefined ? durationMs : this.getCurrentTime();
        
        const totalSeconds = Math.floor(timeToFormat / 1000);
        const seconds = totalSeconds % 60;
        const totalMinutes = Math.floor(totalSeconds / 60);
        const minutes = totalMinutes % 60;
        const hours = Math.floor(totalMinutes / 60);
        
        const hh = String(hours).padStart(2, "0");
        const mm = String(minutes).padStart(2, "0");
        const ss = String(seconds).padStart(2, "0");
        
        return `${hh}:${mm}:${ss}`;
    }

    /**
     * Save current session to sync with server
     */
    async syncWithServer(): Promise<void> {
        if (!this.currentSession || !this.syncCallback) {
            return;
        }

        const totalTime = this.getCurrentTime();
        const taskId = this.currentSession.taskId;

        try {
            await this.syncCallback(taskId, totalTime);
            
            // Update last sync time
            const storedData = this.getStoredTimeData();
            if (storedData[taskId.toString()]) {
                storedData[taskId.toString()].lastSyncTime = Date.now();
                storedData[taskId.toString()].totalTime = totalTime;
                storedData[taskId.toString()].pendingTime = 0;
                this.saveStoredTimeData(storedData);
            }
            
            this.logger.debug(`Synced time with server: ${totalTime}ms`);
        } catch (error) {
            this.logger.warn(`Failed to sync time with server:`, error);
            
            // Mark time as pending sync
            const storedData = this.getStoredTimeData();
            if (storedData[taskId.toString()]) {
                storedData[taskId.toString()].pendingTime = totalTime - storedData[taskId.toString()].totalTime;
                this.saveStoredTimeData(storedData);
            }
        }
    }

    /**
     * Force save current state for page unload
     */
    async saveForUnload(): Promise<void> {
        if (this.currentSession) {
            this.pauseTracking();
            this.saveSessionToStorage();
            
            // Try to sync with server
            if (this.syncCallback) {
                const totalTime = this.getCurrentTime();
                try {
                    await this.syncCallback(this.currentSession.taskId, totalTime);
                } catch (error) {
                    this.logger.warn('Failed to sync during unload:', error);
                }
            }
        }
    }

    /**
     * Clean up resources
     */
    destroy() {
        if (this.syncInterval) {
            clearInterval(this.syncInterval);
            this.syncInterval = null;
        }
        
        if (this.currentSession) {
            this.pauseTracking();
            this.saveSessionToStorage();
        }
        
        this.removeVisibilityHandlers();
        this.removeUnloadHandlers();
    }

    // Private methods

    private setupVisibilityHandlers() {
        document.addEventListener('visibilitychange', this.handleVisibilityChange);
        window.addEventListener('focus', this.handleFocus);
        window.addEventListener('blur', this.handleBlur);
    }

    private removeVisibilityHandlers() {
        document.removeEventListener('visibilitychange', this.handleVisibilityChange);
        window.removeEventListener('focus', this.handleFocus);
        window.removeEventListener('blur', this.handleBlur);
    }

    private setupUnloadHandlers() {
        window.addEventListener('beforeunload', this.handleBeforeUnload);
        window.addEventListener('pagehide', this.handlePageHide);
        window.addEventListener('unload', this.handleUnload);
    }

    private removeUnloadHandlers() {
        window.removeEventListener('beforeunload', this.handleBeforeUnload);
        window.removeEventListener('pagehide', this.handlePageHide);
        window.removeEventListener('unload', this.handleUnload);
    }

    private startPeriodicSync() {
        // Sync with server every 30 seconds
        this.syncInterval = window.setInterval(() => {
            this.syncWithServer();
        }, 30000);
    }

    private saveSessionToStorage() {
        if (!this.currentSession) {
            return;
        }

        const totalTime = this.getCurrentTime();
        this.updateStoredTimeData(this.currentSession.taskId, {
            totalTime,
            sessionStartTime: this.currentSession.startTime,
            lastSyncTime: Date.now(),
            pendingTime: 0
        });
    }

    private getStoredTimeData(): PersistentTimeData {
        try {
            const stored = localStorage.getItem(this.localStorageKey);
            return stored ? JSON.parse(stored) : {};
        } catch (error) {
            this.logger.warn('Failed to read stored time data:', error);
            return {};
        }
    }

    private saveStoredTimeData(data: PersistentTimeData) {
        try {
            localStorage.setItem(this.localStorageKey, JSON.stringify(data));
        } catch (error) {
            this.logger.warn('Failed to save time data to localStorage:', error);
        }
    }

    private updateStoredTimeData(taskId: number, update: Partial<PersistentTimeData[string]>) {
        const data = this.getStoredTimeData();
        data[taskId.toString()] = { ...data[taskId.toString()], ...update };
        this.saveStoredTimeData(data);
    }

    private clearStoredTimeData(taskId: number) {
        const data = this.getStoredTimeData();
        delete data[taskId.toString()];
        this.saveStoredTimeData(data);
    }
}