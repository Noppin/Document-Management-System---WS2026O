import { Box, Button, Card, CardContent, Chip, Stack, Typography } from '@mui/material';
import DescriptionRoundedIcon from '@mui/icons-material/DescriptionRounded';

export default function App() {
  return (
    <Box sx={{ minHeight: '100vh', display: 'grid', placeItems: 'center', p: 3 }}>
      <Card sx={{ width: 'min(720px, 100%)' }}>
        <CardContent sx={{ p: { xs: 3, md: 5 } }}>
          <Stack spacing={3} alignItems="flex-start">
            <Chip icon={<DescriptionRoundedIcon />} label="Sprint 2 · React" color="primary" />
            <Typography variant="h4">Document Management System</Typography>
            <Typography color="text.secondary">
              The React frontend is ready. Dashboard, upload, document details and collections are added in the next Sprint 2 tickets.
            </Typography>
            <Button variant="contained" disabled>Frontend foundation ready</Button>
          </Stack>
        </CardContent>
      </Card>
    </Box>
  );
}
