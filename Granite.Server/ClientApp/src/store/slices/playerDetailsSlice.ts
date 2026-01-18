import { createSlice } from '@reduxjs/toolkit';
import type { PayloadAction } from '@reduxjs/toolkit';
import { PlayerService } from '../../services/PlayerService'
import type { PlayerDetailsDTO } from '../../types/PlayerDetailsDTO';
import type { AppDispatch } from '../../store/store';

interface PlayerDetailsState {
    playerDetails: PlayerDetailsDTO | null;
    loading: boolean;
    error: string | null;
}

const initialState: PlayerDetailsState = {
    playerDetails: null,
    loading: false,
    error: null,
};

const playerDetailsSlice = createSlice({
    name: 'playerDetails',
    initialState,
    reducers: {
        fetchPlayerDetailsStart(state: PlayerDetailsState) {
            state.loading = true;
            state.error = null;
        },
        fetchPlayerDetailsSuccess(state: PlayerDetailsState, action: PayloadAction<PlayerDetailsDTO>) {
            state.loading = false;
            state.playerDetails = action.payload;
        },
        fetchPlayerDetailsFailure(state: PlayerDetailsState, action: PayloadAction<string>) {
            state.loading = false;
            state.error = action.payload;
        },
        clearPlayerDetails(state: PlayerDetailsState) {
            state.playerDetails = null;
        },
    },
});

export const { fetchPlayerDetailsStart, fetchPlayerDetailsSuccess, fetchPlayerDetailsFailure, clearPlayerDetails } = playerDetailsSlice.actions;

export const fetchPlayerDetails = (playerId: string) => async (dispatch: AppDispatch) => {
    dispatch(fetchPlayerDetailsStart());
    try {
        const playerDetails = await PlayerService.getPlayerDetails(playerId);
        dispatch(fetchPlayerDetailsSuccess(playerDetails));
    } catch (error: any) {
        dispatch(fetchPlayerDetailsFailure(error.message));
    }
};

export default playerDetailsSlice.reducer;
