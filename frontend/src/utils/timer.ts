export class Timer {
    public isRunning: boolean = false;
    public isPaused: boolean = false;
    
    private startTime: number | null = null;
    private stopTime: number | null = null;
    private pausedTime: number = 0; // Accumulates total paused duration for the current timing session
    private pauseStartTime: number | null = null; // Timestamp when pause began

    constructor() {}

    /**
     * Starts or resumes the timer.
     * If called when already running and not paused, it will restart the timer.
     */
    public start(): void {
        if (this.isRunning && !this.isPaused) {
            // If already running, effectively restart
            this.reset();
        }

        if (this.isPaused) {
            // Resuming from a paused state
            if (this.pauseStartTime) {
                this.pausedTime += Date.now() - this.pauseStartTime;
                this.pauseStartTime = null;
            }
            this.isPaused = false;
        } else {
            // Starting fresh or restarting
            this.startTime = Date.now();
            this.pausedTime = 0; // Reset accumulated paused time for a new start
        }

        this.isRunning = true;
        this.stopTime = null; // Clear any previous stop time
    }

    /**
     * Pauses the timer.
     */
    public pause(): void {
        if (!this.isRunning || this.isPaused) {
            return;
        }
        this.pauseStartTime = Date.now();
        this.isPaused = true;
    }

    /**
     * Stops the timer and calculates the total elapsed time.
     * @returns The total elapsed time in milliseconds for the session.
     */
    public stop(): number {
        if (!this.isRunning) {
            // If already stopped or never started, return the last known elapsed time
            return this.getElapsedTime();
        }

        this.stopTime = Date.now();

        if (this.isPaused && this.pauseStartTime) {
            // If stopped while paused, account for the current pause duration up to the point of pausing
            this.pausedTime += this.stopTime - this.pauseStartTime;
            this.pauseStartTime = null; // Clear pause start as we are stopping
        }

        this.isRunning = false;
        this.isPaused = false; // Ensure not in paused state after stopping
        const elapsedTime = this.getElapsedTime();
        return elapsedTime;
    }

    /**
     * Resets the timer to its initial state.
     */
    public reset(): void {
        this.startTime = null;
        this.stopTime = null;
        this.pausedTime = 0;
        this.pauseStartTime = null;
        this.isRunning = false;
        this.isPaused = false;
    }

    /**
     * Gets the elapsed time in milliseconds.
     * If the timer is running, it provides the current elapsed time.
     * If stopped, it provides the total elapsed time of the last session.
     * If paused, it provides the elapsed time up to the point it was paused.
     * @returns Elapsed time in milliseconds.
     */
    public getElapsedTime(): number {
        if (!this.startTime) {
            return 0;
        }

        if (this.isPaused && this.pauseStartTime) {
            return this.pauseStartTime - this.startTime - this.pausedTime;
        }

        if (!this.isRunning && this.stopTime) {
            return this.stopTime - this.startTime - this.pausedTime;
        }

        if (this.isRunning) {
            return Date.now() - this.startTime - this.pausedTime;
        }

        return 0;
    }

    /**
     * Formats a duration in milliseconds into HH:MM:SS.ms string.
     * @param durationMs - The duration in milliseconds. If not provided, uses current elapsed time.
     * @param includeMilliseconds - Whether to include milliseconds in the formatted string.
     * @returns Formatted time string.
     */
    public getFormattedElapsedTime(durationMs?: number, includeMilliseconds: boolean = false): string {
        const timeToFormat =
            durationMs !== undefined ? durationMs : this.getElapsedTime();

        const milliseconds = Math.floor((timeToFormat % 1000) / 10); // Get 2 digits for milliseconds
        const totalSeconds = Math.floor(timeToFormat / 1000);
        const seconds = totalSeconds % 60;
        const totalMinutes = Math.floor(totalSeconds / 60);
        const minutes = totalMinutes % 60;
        const hours = Math.floor(totalMinutes / 60);

        const hh = String(hours).padStart(2, "0");
        const mm = String(minutes).padStart(2, "0");
        const ss = String(seconds).padStart(2, "0");
        const ms = String(milliseconds).padStart(2, "0");

        return `${hh}:${mm}:${ss}${includeMilliseconds ? `.${ms}` : ""}`;
    }
}
