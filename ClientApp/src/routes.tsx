import { createBrowserRouter } from "react-router-dom";
import DashboardLayout from "./layouts/Dashboard";
import OverviewPage from "./pages/OverviewPage";
import PlayersPage from "./pages/PlayersPage";

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
        }
    ]

};


const router = createBrowserRouter([MainRoutes], { basename: import.meta.env.VITE_APP_BASE_NAME });

export default router;