import { createSlice } from '@reduxjs/toolkit';
import type { PayloadAction } from '@reduxjs/toolkit';
import WorldService from '../../services/WorldService';
import type { CollectibleObjectDTO } from '../../types/CollectibleObjectDTO';

interface WorldState {
    collectibles: {
        items: CollectibleObjectDTO[];
        loading: boolean;
        error: string | null;
        loaded: boolean; // Track if data has been loaded to avoid redundant fetches
    };
}

const initialState: WorldState = {
    collectibles: {
        items: [],
        loading: false,
        error: null,
        loaded: false,
    },
};

const worldSlice = createSlice({
    name: 'world',
    initialState,
    reducers: {
        fetchCollectiblesStart(state) {
            state.collectibles.loading = true;
            state.collectibles.error = null;
        },
        fetchCollectiblesSuccess(state, action: PayloadAction<CollectibleObjectDTO[]>) {
            state.collectibles.loading = false;
            state.collectibles.items = action.payload;
            state.collectibles.loaded = true;
        },
        fetchCollectiblesFailure(state, action: PayloadAction<string>) {
            state.collectibles.loading = false;
            state.collectibles.error = action.payload;
        },
        clearCollectibles(state) {
            state.collectibles.items = [];
            state.collectibles.loaded = false;
            state.collectibles.error = null;
        },
    },
});

export const {
    fetchCollectiblesStart,
    fetchCollectiblesSuccess,
    fetchCollectiblesFailure,
    clearCollectibles,
} = worldSlice.actions;

// Thunk to fetch collectibles only if not already loaded
export const fetchCollectibles = (force: boolean = false) => async (dispatch: any, getState: any) => {
    const { world } = getState();
    
    // Skip if already loaded and not forcing refresh
    if (world.collectibles.loaded && !force) {
        return;
    }

    // Skip if already loading
    if (world.collectibles.loading) {
        return;
    }

    dispatch(fetchCollectiblesStart());
    try {
        const items = await WorldService.GetAllCollectibles();
        dispatch(fetchCollectiblesSuccess(items));
    } catch (error: any) {
        dispatch(fetchCollectiblesFailure(error.message || 'Failed to fetch collectibles'));
    }
};

export default worldSlice.reducer;
