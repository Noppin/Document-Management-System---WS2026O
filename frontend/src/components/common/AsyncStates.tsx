import { Alert, Box, Button, CircularProgress, Stack, Typography } from '@mui/material';
import InboxRoundedIcon from '@mui/icons-material/InboxRounded';

export function LoadingState({ label = 'Loading…' }: { label?: string }) {
  return (
    <Stack alignItems="center" justifyContent="center" spacing={2} sx={{ minHeight: 220 }} role="status">
      <CircularProgress size={34} />
      <Typography color="text.secondary">{label}</Typography>
    </Stack>
  );
}

export function ErrorState({ message, onRetry }: { message: string; onRetry?: () => void }) {
  return (
    <Alert
      severity="error"
      action={onRetry ? <Button color="inherit" size="small" onClick={onRetry}>Retry</Button> : undefined}
    >
      {message}
    </Alert>
  );
}

export function EmptyState({ title, description }: { title: string; description: string }) {
  return (
    <Box sx={{ textAlign: 'center', py: 7, px: 2 }}>
      <InboxRoundedIcon sx={{ fontSize: 44, color: 'text.disabled', mb: 1 }} />
      <Typography variant="h6">{title}</Typography>
      <Typography color="text.secondary" sx={{ mt: 0.5 }}>{description}</Typography>
    </Box>
  );
}
