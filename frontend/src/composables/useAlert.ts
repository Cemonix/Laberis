import { ref, readonly } from 'vue';

const isAlertOpen = ref(false);
const alertTitle = ref('');
const alertMessage = ref('');

let currentPromiseResolve: ((value: boolean) => void) | null = null;
let currentPromiseReject: ((reason?: any) => void) | null = null;
let currentTimeoutId: number | null = null;

export function useAlert() {
    const closeAlertAndSettlePromise = (didConfirm: boolean, rejectionReason?: any) => {
        if (currentTimeoutId) {
            clearTimeout(currentTimeoutId);
            currentTimeoutId = null;
        }

        isAlertOpen.value = false;

        if (rejectionReason && currentPromiseReject) {
            currentPromiseReject(rejectionReason);
        } else if (currentPromiseResolve) {
            currentPromiseResolve(didConfirm);
        }

        // Reset promise handlers for the next alert
        currentPromiseResolve = null;
        currentPromiseReject = null;
    };

    const showAlert = (
        title: string, 
        message: string,
        timeoutDuration?: number
    ): Promise<boolean> => {
        // If an alert is already open, reject its promise as it's being superseded
        if (isAlertOpen.value && currentPromiseReject) {
            // Clear any existing timeout for the superseded alert
            if (currentTimeoutId) {
                clearTimeout(currentTimeoutId);
                currentTimeoutId = null;
            }
            currentPromiseReject('Alert superseded by a new alert.');
        }

        alertTitle.value = title;
        alertMessage.value = message;
        isAlertOpen.value = true;

        return new Promise<boolean>((resolve, reject) => {
            currentPromiseResolve = resolve;
            currentPromiseReject = reject;

            if (timeoutDuration) {
                // Capture the current resolve function for this specific timeout
                const resolveForThisTimeout = resolve;
                currentTimeoutId = window.setTimeout(() => {
                    // Ensure this timeout corresponds to the currently active promise
                    if (currentPromiseResolve === resolveForThisTimeout) {
                        closeAlertAndSettlePromise(true);
                    }
                }, timeoutDuration);
            }
        });
    };

    const handleAlertConfirm = () => {
        if (isAlertOpen.value) {
            closeAlertAndSettlePromise(true);
        }
    };
    
    return {
        isAlertOpen: readonly(isAlertOpen),
        alertTitle: readonly(alertTitle),
        alertMessage: readonly(alertMessage),
        showAlert,
        handleAlertConfirm
    };
}