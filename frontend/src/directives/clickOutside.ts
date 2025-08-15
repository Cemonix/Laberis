import type { DirectiveBinding } from "vue";

interface ClickOutsideElement extends HTMLElement {
    __clickOutsideHandler?: (event: Event) => void;
}

/**
 * Click outside directive for Vue 3
 * Usage: v-click-outside="handler"
 *
 * The handler function will be called when a click occurs outside the element
 */
export const clickOutside = {
    mounted(el: ClickOutsideElement, binding: DirectiveBinding) {
        const handler = (event: Event) => {
            // Check if the click was outside the element
            if (el && !el.contains(event.target as Node)) {
                // Call the provided handler function
                if (typeof binding.value === "function") {
                    binding.value(event);
                }
            }
        };

        // Store the handler on the element for cleanup
        el.__clickOutsideHandler = handler;

        // Add event listener to document
        document.addEventListener("click", handler, true);
    },

    beforeUnmount(el: ClickOutsideElement) {
        // Clean up the event listener
        if (el.__clickOutsideHandler) {
            document.removeEventListener(
                "click",
                el.__clickOutsideHandler,
                true
            );
            delete el.__clickOutsideHandler;
        }
    },
};

export default clickOutside;
