/**
 * Interface for timer service functionality
 * Implemented by TimeTracker for consistency with existing code
 */
export interface TimerService {
    isRunning(): boolean;
    getElapsedTime(): number;
    start(): void;
    stop(): void;
    pause(): void;
    resume(): void;
    reset(): void;
    getFormattedElapsedTime(durationMs?: number): string;
}