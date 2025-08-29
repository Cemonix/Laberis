import { useToast } from '@/composables/useToast';
import { useAlert } from '@/composables/useAlert';
import { AppLogger } from '@/core/logger/logger';
import {
    AppError,
    NetworkError,
    ServerError,
    ValidationError,
    ToolError,
    UserVisibleError
} from '@/core/errors/errors';
import { useAuthErrorHandler } from './useAuthErrorHandler';

const logger = AppLogger.createServiceLogger('ErrorHandler');

// A type alias for the UI feedback functions to keep handler signatures clean.
type ErrorFeedbackUI = {
    showError: (title: string, message: string) => void;
    showWarning: (title: string, message: string) => void;
    showAlert: (title: string, message: string) => Promise<boolean>;
};

/**
 * Handles high-priority errors that require a blocking modal/alert.
 * @returns `true` if the error was handled, `false` otherwise.
 */
const handleAlertableError = (error: unknown, ui: ErrorFeedbackUI): boolean => {
    if (error instanceof UserVisibleError || error instanceof ToolError) {
        ui.showAlert(error.alertTitle, error.message);
        return true;
    }
    return false;
};

/**
 * Handles authentication-related errors, specifically 401 Unauthorized.
 * This is a specialized handler that uses the auth error handler composable.
 * @returns `true` if the error was handled, `false` otherwise.
 */
const handleAuthError = (error: unknown): boolean => {
    // We only care about ServerError with status 401
    if (error instanceof ServerError && error.statusCode === 401) {
        const authErrorHandler = useAuthErrorHandler();
        authErrorHandler.handleAuthError(error, 'Auth Pipeline Handler');
        return true;
    }
    return false;
};

/**
 * Handles errors originating from the backend server.
 * @returns `true` if the error was handled, `false` otherwise.
 */
const handleServerError = (error: unknown, ui: ErrorFeedbackUI): boolean => {
    if (!(error instanceof ServerError)) return false;

    if (error.statusCode && error.statusCode >= 500) {
        ui.showError('Server Error', 'The server encountered a problem. Please try again later.');
    } else if (error.statusCode === 403) {
        ui.showError('Access Denied', 'You do not have permission to perform this action.');
    } else {
        ui.showError('Request Error', error.message || 'The server could not process your request.');
    }
    return true;
};

/**
 * Handles network-level connectivity errors.
 * @returns `true` if the error was handled, `false` otherwise.
 */
const handleNetworkError = (error: unknown, ui: ErrorFeedbackUI): boolean => {
    if (!(error instanceof NetworkError)) return false;
    ui.showError('Network Error', error.message || 'Please check your internet connection.');
    return true;
};

/**
 * Handles user input validation errors.
 * @returns `true` if the error was handled, `false` otherwise.
 */
const handleValidationError = (error: unknown, ui: ErrorFeedbackUI): boolean => {
    if (!(error instanceof ValidationError)) return false;
    ui.showWarning('Validation Error', error.message);
    return true;
};

/**
 * Handles generic, known application errors that don't fit other categories.
 * @returns `true` if the error was handled, `false` otherwise.
 */
const handleGenericAppError = (error: unknown, ui: ErrorFeedbackUI): boolean => {
    // This check must come after its more specific children (ServerError, NetworkError, etc.)
    if (!(error instanceof AppError)) return false;
    ui.showError('Application Error', error.message);
    return true;
};

/**
 * The final handler in the chain to catch any remaining errors.
 * @returns `true` as it is the final fallback.
 */
const handleFallbackError = (error: unknown, ui: ErrorFeedbackUI): boolean => {
    if (error instanceof Error) {
        ui.showError('An Unexpected Error Occurred', 'Please try again. If the problem persists, contact support.');
    } else {
        ui.showError('An Unknown Error Occurred', 'An unknown error was caught by the system.');
    }
    return true;
};


/**
 * A centralized composable for handling errors throughout the application.
 * It logs every error and provides consistent UI feedback using a pipeline of handlers.
 * @returns An object containing the `handleError` function.
 */
export function useErrorHandler() {
    const { showError, showWarning } = useToast();
    const { showAlert } = useAlert();
    const ui: ErrorFeedbackUI = { showError, showWarning, showAlert };

    // The pipeline is an array of handler functions, ordered by specificity.
    const errorHandlingPipeline = [
        handleAlertableError,
        handleAuthError,
        handleServerError,
        handleNetworkError,
        handleValidationError,
        handleGenericAppError, // Generic handler is placed after more specific ones.
        handleFallbackError,   // Fallback handler is always last.
    ];

    /**
     * Processes an error, logs it, and shows appropriate UI feedback.
     * @param error The error object, typed as `unknown` to be safe.
     * @param context An optional string to provide more context for logging.
     */
    const handleError = (error: unknown, context?: string) => {
        const logMessage = context ? `Error in ${context}` : 'An error occurred';
        logger.error(logMessage, error);

        for (const handler of errorHandlingPipeline) {
            if (handler(error, ui)) {
                // Stop processing once a handler has successfully dealt with the error
                break;
            }
        }
    };

    return {
        handleError
    };
}