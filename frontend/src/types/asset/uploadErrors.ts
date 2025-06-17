/**
 * Asset-specific upload error classes
 */

export class UploadError extends Error {
    constructor(message: string, public readonly cause?: Error) {
        super(message);
        this.name = 'UploadError';
        
        // Maintain proper stack trace for where our error was thrown (only available on V8)
        if (Error.captureStackTrace) {
            Error.captureStackTrace(this, UploadError);
        }
    }
}

export class NoFilesProvidedError extends UploadError {
    constructor() {
        super('No files were provided for upload');
        this.name = 'NoFilesProvidedError';
    }
}

export class InvalidFileTypeError extends UploadError {
    constructor(filename: string, supportedTypes: string[] = []) {
        const typesMessage = supportedTypes.length > 0 
            ? ` Supported types: ${supportedTypes.join(', ')}`
            : '';
        super(`File "${filename}" has an invalid file type.${typesMessage}`);
        this.name = 'InvalidFileTypeError';
    }
}

export class FileTooLargeError extends UploadError {
    constructor(filename: string, maxSize: number) {
        super(`File "${filename}" exceeds the maximum allowed size of ${Math.round(maxSize / 1024 / 1024)}MB`);
        this.name = 'FileTooLargeError';
    }
}
