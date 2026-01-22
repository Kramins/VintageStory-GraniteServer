export interface ModDTO {
    modId: string;
    name: string;
    description: string;
    author: string;
    runningVersion?: string | null;
    installedVersion?: string | null;
}
