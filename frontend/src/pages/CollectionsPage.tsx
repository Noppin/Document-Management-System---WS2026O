import { Card, CardContent, Stack, Typography } from '@mui/material';

export function CollectionsPage() {
  return (
    <Stack spacing={3}>
      <div><Typography variant="h4">Collections</Typography><Typography color="text.secondary">Organize related documents.</Typography></div>
      <Card><CardContent><Typography color="text.secondary">Collection management is added in S2-07.</Typography></CardContent></Card>
    </Stack>
  );
}
