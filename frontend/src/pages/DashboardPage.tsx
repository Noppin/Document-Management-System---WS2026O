import { Card, CardContent, Stack, Typography } from '@mui/material';

export function DashboardPage() {
  return (
    <Stack spacing={3}>
      <div>
        <Typography variant="h4">Dashboard</Typography>
        <Typography color="text.secondary">Your document workspace at a glance.</Typography>
      </div>
      <Card><CardContent><Typography color="text.secondary">Document statistics and recent uploads will appear here.</Typography></CardContent></Card>
    </Stack>
  );
}
