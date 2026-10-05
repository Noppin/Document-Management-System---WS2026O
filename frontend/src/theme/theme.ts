import { alpha, createTheme } from '@mui/material/styles';

export const theme = createTheme({
  palette: {
    mode: 'light',
    primary: {
      main: '#4f46e5',
      light: '#818cf8',
      dark: '#3730a3',
    },
    secondary: {
      main: '#0f766e',
    },
    background: {
      default: '#f7f8fc',
      paper: '#ffffff',
    },
  },
  shape: {
    borderRadius: 14,
  },
  typography: {
    fontFamily: 'Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif',
    h4: { fontWeight: 800, letterSpacing: '-0.03em' },
    h5: { fontWeight: 750, letterSpacing: '-0.02em' },
    h6: { fontWeight: 700 },
    button: { textTransform: 'none', fontWeight: 700 },
  },
  components: {
    MuiCard: {
      styleOverrides: {
        root: ({ theme }) => ({
          border: `1px solid ${alpha(theme.palette.divider, 0.65)}`,
          boxShadow: '0 12px 35px rgba(15, 23, 42, 0.05)',
        }),
      },
    },
    MuiButton: {
      defaultProps: { disableElevation: true },
    },
  },
});
