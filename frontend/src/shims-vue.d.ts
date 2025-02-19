// This file is used to tell TypeScript that .vue files are valid modules.
declare module '*.vue' {
    import { DefineComponent } from 'vue';
    const component: DefineComponent<{}, {}, any>;
    export default component;
}