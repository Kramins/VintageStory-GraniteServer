import { Outlet } from "react-router-dom";
import { useState } from "react";

import { alpha } from "@mui/material/styles";

import Stack from "@mui/material/Stack";
import Box from "@mui/material/Box";
import Header from "../components/Header";
import SideMenu from "../components/SideMenu";
import { Grid } from "@mui/material";
import AppNavbar from "../components/AppNavbar";

export default function DashboardLayout() {
  const [drawerOpen, setDrawerOpen] = useState(false);

  return (
    <Box sx={{ display: "flex" }}>
      <SideMenu />
      <AppNavbar />
      {/* Main content */}
      <Box
        component="main"
        sx={(theme) => ({
          height: "100vh",
          flexGrow: 1,
          backgroundColor: theme.vars
            ? `rgba(${theme.vars.palette.background.defaultChannel} / 1)`
            : alpha(theme.palette.background.default, 1),
          overflow: "auto",
        })}
      >
        <Stack
          spacing={2}
          sx={{
            alignItems: "center",
            mx: 3,
            pb: 5,
            mt: { xs: 8, md: 0 },
          }}
        >
          <Header />
          <Outlet />
        </Stack>
      </Box>
    </Box>
  );
}
