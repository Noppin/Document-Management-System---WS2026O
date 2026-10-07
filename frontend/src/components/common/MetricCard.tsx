import { ReactNode } from 'react';
import { Box, Card, CardContent, Stack, Typography } from '@mui/material';

export function MetricCard({ label, value, icon }: { label: string; value: string; icon: ReactNode }) {
  return (
    <Card>
      <CardContent>
        <Stack direction="row" justifyContent="space-between" alignItems="center">
          <Box>
            <Typography variant="body2" color="text.secondary" fontWeight={600}>{label}</Typography>
            <Typography variant="h5" sx={{ mt: 0.5 }}>{value}</Typography>
          </Box>
          <Box sx={{ width: 44, height: 44, borderRadius: 3, display: 'grid', placeItems: 'center', bgcolor: 'primary.50', color: 'primary.main' }}>
            {icon}
          </Box>
        </Stack>
      </CardContent>
    </Card>
  );
}
