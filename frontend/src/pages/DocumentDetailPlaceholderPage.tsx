import {
  Alert,
  Button,
  Stack,
  Typography,
} from '@mui/material';
import {
  Link as RouterLink,
  useParams,
} from 'react-router-dom';

export function DocumentDetailPlaceholderPage() {
  const { id } = useParams();

  return (
    <Stack spacing={3}>
      <div>
        <Typography variant="h4">
          Document details
        </Typography>

        <Typography color="text.secondary">
          Document route: {id ?? 'unknown'}
        </Typography>
      </div>

      <Alert severity="info">
        The document detail route is ready.
        The complete document detail functionality will be implemented later on.
      </Alert>

      <Button
        component={RouterLink}
        to="/"
        variant="outlined"
        sx={{ alignSelf: 'flex-start' }}
      >
        Back to dashboard
      </Button>
    </Stack>
  );
}