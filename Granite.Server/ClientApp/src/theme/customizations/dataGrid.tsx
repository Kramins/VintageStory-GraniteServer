import { type Theme, alpha } from '@mui/material/styles';

/* eslint-disable import/prefer-default-export */
export const getDataGridCustomizations = (_gray: any, _brand: any): any => ({
  MuiDataGrid: {
    styleOverrides: {
      root: ({ theme }: { theme: Theme }) => ({
        border: `1px solid ${(theme.vars || theme).palette.divider}`,
        borderRadius: (theme.vars || theme).shape.borderRadius,
        '& .MuiDataGrid-withBorderColor': {
          borderColor: (theme.vars || theme).palette.divider,
        },
      }),
      cell: ({ theme }: { theme: Theme }) => ({
        borderColor: (theme.vars || theme).palette.divider,
        color: (theme.vars || theme).palette.text.primary,
      }),
      row: ({ theme }: { theme: Theme }) => ({
        '&:hover': {
          backgroundColor: alpha(theme.palette.primary.main, 0.04),
        },
        '&.Mui-selected': {
          backgroundColor: alpha(theme.palette.primary.main, 0.08),
          '&:hover': {
            backgroundColor: alpha(theme.palette.primary.main, 0.12),
          },
        },
      }),
      columnHeaders: ({ theme }: { theme: Theme }) => ({
        backgroundColor: (theme.vars || theme).palette.background.paper,
        borderColor: (theme.vars || theme).palette.divider,
      }),
      columnHeader: () => ({
        '&:focus, &:focus-within': {
          outline: 'none',
        },
      }),
      columnHeaderTitle: ({ theme }: { theme: Theme }) => ({
        fontWeight: 600,
        color: (theme.vars || theme).palette.text.primary,
      }),
      footerContainer: ({ theme }: { theme: Theme }) => ({
        borderColor: (theme.vars || theme).palette.divider,
        backgroundColor: (theme.vars || theme).palette.background.paper,
      }),
      iconButtonContainer: ({ theme }: { theme: Theme }) => ({
        '& button': {
          color: (theme.vars || theme).palette.text.secondary,
        },
      }),
      menuIcon: ({ theme }: { theme: Theme }) => ({
        color: (theme.vars || theme).palette.text.secondary,
      }),
      sortIcon: ({ theme }: { theme: Theme }) => ({
        color: (theme.vars || theme).palette.text.secondary,
      }),
      columnSeparator: ({ theme }: { theme: Theme }) => ({
        color: (theme.vars || theme).palette.divider,
      }),
    },
  },
});
