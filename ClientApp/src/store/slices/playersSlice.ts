import { createSlice } from '@reduxjs/toolkit';
import type { PayloadAction } from '@reduxjs/toolkit';
import { PlayerManagementService } from '../../services/PlayerManagementService';
import type { AppDispatch } from '../../store/store';

interface PlayerState {
    players: any[]; // Replace `any` with the appropriate Player type
    loading: boolean;
    error: string | null;
}

const initialState: PlayerState = {
    players: [],
    loading: false,
    error: null,
};

const playersSlice = createSlice({
    name: 'players',
    initialState,
    reducers: {
        fetchPlayersStart(state: PlayerState) {
            state.loading = true;
            state.error = null;
        },
        fetchPlayersSuccess(state: PlayerState, action: PayloadAction<any[]>) {
            state.loading = false;
            state.players = action.payload;
        },
        fetchPlayersFailure(state: PlayerState, action: PayloadAction<string>) {
            state.loading = false;
            state.error = action.payload;
        },
    },
});

export const { fetchPlayersStart, fetchPlayersSuccess, fetchPlayersFailure } = playersSlice.actions;

export const fetchPlayers = () => async (dispatch: AppDispatch) => {
    dispatch(fetchPlayersStart());
    try {
        const players = await PlayerManagementService.getPlayers();
        dispatch(fetchPlayersSuccess(players));
    } catch (error: any) {
        dispatch(fetchPlayersFailure(error.message));
    }
};

export default playersSlice.reducer;