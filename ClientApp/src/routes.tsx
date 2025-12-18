import { createBrowserRouter } from "react-router-dom";
import DashboardLayout from "./layouts/Dashboard";
import OverviewPage from "./pages/OverviewPage";
import PlayersPage from "./pages/PlayersPage";
import PlayerDetailsPage from "./pages/PlayerDetailsPage";

const MainRoutes = {
    path: '/',
    element: <DashboardLayout />,
    children: [
        {
            path: '/',
            element: <OverviewPage />
        },
        {
            path: '/players',
            element: <PlayersPage />
        },
        {
            path: '/players/:playerId',
            element: <PlayerDetailsPage />
        }
    ]

};


const router = createBrowserRouter([MainRoutes], { basename: import.meta.env.VITE_APP_BASE_NAME });

export default router;