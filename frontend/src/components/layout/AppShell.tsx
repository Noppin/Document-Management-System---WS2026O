import { PropsWithChildren, useState } from 'react';
import { NavLink, useLocation } from 'react-router-dom';
import {
  AppBar,
  Box,
  Button,
  Divider,
  Drawer,
  IconButton,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Stack,
  Toolbar,
  Typography,
  useMediaQuery,
  useTheme,
} from '@mui/material';
import DashboardRoundedIcon from '@mui/icons-material/DashboardRounded';
import UploadFileRoundedIcon from '@mui/icons-material/UploadFileRounded';
import FolderRoundedIcon from '@mui/icons-material/FolderRounded';
import DescriptionRoundedIcon from '@mui/icons-material/DescriptionRounded';
import MenuRoundedIcon from '@mui/icons-material/MenuRounded';
import LogoutRoundedIcon from '@mui/icons-material/LogoutRounded';
import { getUsername, logout } from '../../auth/keycloak';

const drawerWidth = 248;

const navigation = [
  { label: 'Dashboard', to: '/', icon: <DashboardRoundedIcon /> },
  { label: 'Upload', to: '/upload', icon: <UploadFileRoundedIcon /> },
  { label: 'Collections', to: '/collections', icon: <FolderRoundedIcon /> },
];

function Navigation() {
  const location = useLocation();
  const username = getUsername();
  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
      <Toolbar sx={{ px: 2.5 }}>
        <Stack direction="row" spacing={1.5} alignItems="center">
          <Box sx={{ width: 38, height: 38, borderRadius: 2.5, bgcolor: 'primary.main', color: 'primary.contrastText', display: 'grid', placeItems: 'center' }}>
            <DescriptionRoundedIcon />
          </Box>
          <Box>
            <Typography fontWeight={800} lineHeight={1.1}>DMS</Typography>
            <Typography variant="caption" color="text.secondary">Document workspace</Typography>
          </Box>
        </Stack>
      </Toolbar>
      <Divider />
      <List sx={{ p: 1.5, flexGrow: 1 }}>
        {navigation.map((item) => {
          const selected = item.to === '/' ? location.pathname === '/' : location.pathname.startsWith(item.to);
          return (
            <ListItemButton
              key={item.to}
              component={NavLink}
              to={item.to}
              selected={selected}
              sx={{ borderRadius: 2.5, mb: 0.5 }}
            >
              <ListItemIcon sx={{ minWidth: 40 }}>{item.icon}</ListItemIcon>
              <ListItemText primary={item.label} primaryTypographyProps={{ fontWeight: selected ? 700 : 500 }} />
            </ListItemButton>
          );
        })}
      </List>
      <Divider />
      <Box sx={{ p: 1.5 }}>
        {username && (
          <Typography variant="body2" color="text.secondary" noWrap sx={{ px: 1, mb: 1 }}>
            {username}
          </Typography>
        )}
        <Button fullWidth onClick={logout} startIcon={<LogoutRoundedIcon />} sx={{ justifyContent: 'flex-start' }}>
          Logout
        </Button>
      </Box>
    </Box>
  );
}

export function AppShell({ children }: PropsWithChildren) {
  const theme = useTheme();
  const desktop = useMediaQuery(theme.breakpoints.up('md'));
  const [mobileOpen, setMobileOpen] = useState(false);

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh' }}>
      <AppBar
        position="fixed"
        color="inherit"
        elevation={0}
        sx={{ display: { md: 'none' }, borderBottom: 1, borderColor: 'divider' }}
      >
        <Toolbar>
          <IconButton edge="start" onClick={() => setMobileOpen(true)} aria-label="Open navigation">
            <MenuRoundedIcon />
          </IconButton>
          <Typography fontWeight={800} sx={{ ml: 1 }}>Document Management System</Typography>
        </Toolbar>
      </AppBar>

      <Box component="nav" sx={{ width: { md: drawerWidth }, flexShrink: { md: 0 } }}>
        <Drawer
          variant={desktop ? 'permanent' : 'temporary'}
          open={desktop || mobileOpen}
          onClose={() => setMobileOpen(false)}
          ModalProps={{ keepMounted: true }}
          sx={{ '& .MuiDrawer-paper': { width: drawerWidth, boxSizing: 'border-box', borderRightColor: 'divider' } }}
        >
          <Navigation />
        </Drawer>
      </Box>

      <Box component="main" sx={{ flexGrow: 1, minWidth: 0, pt: { xs: 10, md: 0 }, p: { xs: 2, sm: 3, lg: 4 } }}>
        <Box sx={{ maxWidth: 1280, mx: 'auto' }}>{children}</Box>
      </Box>
    </Box>
  );
}
