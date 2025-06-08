import { ref, readonly } from 'vue';

const isAlertOpen = ref(false);
const alertTitle = ref('');
const alertMessage = ref('');
let resolvePromise: (value: boolean) => void;
let timeoutId: number | null = null;

export function useAlert() {
    const showAlert = (
        title: string, 
        message: string,
        timeoutDuration?: number
    ): Promise<boolean> => {
        alertTitle.value = title;
        alertMessage.value = message;
        isAlertOpen.value = true;
        
        if (timeoutDuration) {
            timeoutId = window.setTimeout(() => {
                handleConfirm();
            }, timeoutDuration);
        }

        return new Promise((resolve) => {
            resolvePromise = resolve;
        });
    };

    const handleConfirm = () => {
        if (timeoutId) {
            clearTimeout(timeoutId);
            timeoutId = null;
        }

        isAlertOpen.value = false;
        if (resolvePromise) {
            resolvePromise(true);
        }
    };

    return {
        isAlertOpen: readonly(isAlertOpen),
        alertTitle: readonly(alertTitle),
        alertMessage: readonly(alertMessage),
        showAlert,
        handleConfirm,
    };
}