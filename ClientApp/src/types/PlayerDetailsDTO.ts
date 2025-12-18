import type { PlayerDTO } from "./PlayerDTO";

export interface InventoryDTO {
    name: string;
    slots: InventorySlotDTO[];
}

export interface InventorySlotDTO {
    entityClass: string | null;
    entityId: number;
    name: string | null;
    slotIndex: number;
    stackSize: number;
}

export interface PlayerDetailsDTO extends PlayerDTO {
    inventories: { [key: string]: InventoryDTO; };
}