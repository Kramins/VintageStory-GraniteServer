import type { PlayerDTO } from "./PlayerDTO";

export interface InventoryDTO {
    name: string;
    slots: InventorySlotDTO[];
}

export interface InventorySlotDTO {
    class: string | null;
    id: number;
    name: string | null;
    slotIndex: number;
    stackSize: number;
}

export interface PlayerDetailsDTO extends PlayerDTO {
    inventories: { [key: string]: InventoryDTO; };
}