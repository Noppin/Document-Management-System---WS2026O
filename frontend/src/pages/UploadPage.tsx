import { Card, CardContent, Stack, Typography } from '@mui/material';

export function UploadPage() {
  return (
    <Stack spacing={3}>
      <div><Typography variant="h4">Upload document</Typography><Typography color="text.secondary">Add a PDF to your archive.</Typography></div>
      <Card><CardContent><Typography color="text.secondary">The validated upload form is added later on.</Typography></CardContent></Card>
    </Stack>
  );
}
