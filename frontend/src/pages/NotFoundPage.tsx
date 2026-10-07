import { Button, Card, CardContent, Stack, Typography } from '@mui/material';
import { Link } from 'react-router-dom';

export function NotFoundPage() {
  return (
    <Card sx={{ maxWidth: 640, mx: 'auto', mt: 8 }}>
      <CardContent sx={{ p: 5 }}>
        <Stack spacing={2} alignItems="flex-start">
          <Typography variant="h4">Page not found</Typography>
          <Typography color="text.secondary">The page you requested does not exist.</Typography>
          <Button component={Link} to="/" variant="contained">Back to dashboard</Button>
        </Stack>
      </CardContent>
    </Card>
  );
}
