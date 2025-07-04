import { ref, readonly } from 'vue';

const isConfirmOpen = ref(false);
const confirmTitle = ref('');
const confirmMessage = ref('');

let currentPromiseResolve: ((value: boolean) => void) | null = null;
let currentPromiseReject: ((reason?: any) => void) | null = null;
let currentTimeoutId: number | null = null;

export function useConfirm() {
    const closeConfirmAndSettlePromise = (didConfirm: boolean, rejectionReason?: any) => {
        if (currentTimeoutId) {
            clearTimeout(currentTimeoutId);
            currentTimeoutId = null;
        }

        isConfirmOpen.value = false;

        if (rejectionReason && currentPromiseReject) {
            currentPromiseReject(rejectionReason);
        } else if (currentPromiseResolve) {
            currentPromiseResolve(didConfirm);
        }

        // Reset promise handlers for the next confirm
        currentPromiseResolve = null;
        currentPromiseReject = null;
    };

    const showConfirm = (
        title: string, 
        message: string,
        timeoutDuration?: number
    ): Promise<boolean> => {
        // If a confirm is already open, reject its promise as it's being superseded
        if (isConfirmOpen.value && currentPromiseReject) {
            // Clear any existing timeout for the superseded confirm
            if (currentTimeoutId) {
                clearTimeout(currentTimeoutId);
                currentTimeoutId = null;
            }
            currentPromiseReject('Confirm superseded by a new confirm.');
        }

        confirmTitle.value = title;
        confirmMessage.value = message;
        isConfirmOpen.value = true;

        return new Promise<boolean>((resolve, reject) => {
            currentPromiseResolve = resolve;
            currentPromiseReject = reject;

            if (timeoutDuration) {
                // Capture the current resolve function for this specific timeout
                const resolveForThisTimeout = resolve;
                currentTimeoutId = window.setTimeout(() => {
                    // Ensure this timeout corresponds to the currently active promise
                    if (currentPromiseResolve === resolveForThisTimeout) {
                        closeConfirmAndSettlePromise(true);
                    }
                }, timeoutDuration);
            }
        });
    };

    const handleConfirm = () => {
        if (isConfirmOpen.value) {
            closeConfirmAndSettlePromise(true);
        }
    };

    const handleCancel = () => {
        if (isConfirmOpen.value) {
            closeConfirmAndSettlePromise(false);
        }
    };
    
    return {
        isConfirmOpen: readonly(isConfirmOpen),
        confirmTitle: readonly(confirmTitle),
        confirmMessage: readonly(confirmMessage),
        showConfirm,
        handleConfirm,
        handleCancel
    };
}