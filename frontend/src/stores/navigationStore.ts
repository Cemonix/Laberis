import { defineStore } from "pinia";

export const useNavigationStore = defineStore("navigation", {
    state: () => ({
        isNavigating: false,
        navigationMessage: "",
    }),
    actions: {
        startNavigation(message = "Loading page...") {
            this.isNavigating = true;
            this.navigationMessage = message;
        },
        finishNavigation() {
            this.isNavigating = false;
            this.navigationMessage = "";
        },
    },
});