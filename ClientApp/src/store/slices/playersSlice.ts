import { createSlice } from '@reduxjs/toolkit';
import type { PayloadAction } from '@reduxjs/toolkit';
import { PlayerService } from '../../services/PlayerService'
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
        fetchAllPlayersStart(state: PlayerState) {
            state.loading = true;
            state.error = null;
        },
        fetchAllPlayersSuccess(state: PlayerState, action: PayloadAction<any[]>) {
            state.loading = false;
            state.players = action.payload;
        },
        fetchAllPlayersFailure(state: PlayerState, action: PayloadAction<string>) {
            state.loading = false;
            state.error = action.payload;
        },
    },
});

export const { fetchAllPlayersStart, fetchAllPlayersSuccess, fetchAllPlayersFailure } = playersSlice.actions;

export const fetchAllPlayers = () => async (dispatch: AppDispatch) => {
    dispatch(fetchAllPlayersStart());
    try {
        const players = await PlayerService.getAllPlayers();
        dispatch(fetchAllPlayersSuccess(players));
    } catch (error: any) {
        dispatch(fetchAllPlayersFailure(error.message));
    }
};

export default playersSlice.reducer;