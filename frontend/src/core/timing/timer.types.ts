/**
 * Interface for timer service functionality
 */
export interface TimerService {
    isRunning(): boolean;
    getElapsedTime(): number;
    start(): void;
    stop(): void;
    pause(): void;
    resume(): void;
    reset(): void;
}