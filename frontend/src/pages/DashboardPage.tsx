import { useState } from 'react';
import {
  Button,
  Card,
  CardContent,
  Grid2 as Grid,
  List,
  ListItemButton,
  ListItemText,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import DescriptionRoundedIcon from '@mui/icons-material/DescriptionRounded';
import StorageRoundedIcon from '@mui/icons-material/StorageRounded';
import ScheduleRoundedIcon from '@mui/icons-material/ScheduleRounded';
import UploadRoundedIcon from '@mui/icons-material/UploadRounded';
import SearchRoundedIcon from '@mui/icons-material/SearchRounded';
import { Link } from 'react-router-dom';
import { EmptyState, ErrorState, LoadingState } from '../components/common/AsyncStates';
import { MetricCard } from '../components/common/MetricCard';
import { DocumentTable } from '../features/documents/components/DocumentTable';
import { useDocuments, useSearchDocuments } from '../features/documents/queries';
import { formatBytes, formatDate } from '../utils/format';

export function DashboardPage() {
  const [searchTerm, setSearchTerm] = useState('');
  const documents = useDocuments();
  const search = useSearchDocuments(searchTerm);

  if (documents.isLoading) return <LoadingState label="Loading documents…" />;
  if (documents.isError) return <ErrorState message={documents.error.message} onRetry={() => void documents.refetch()} />;

  const items = documents.data ?? [];
  const totalSize = items.reduce((sum, document) => sum + document.fileSize, 0);
  const latest = items[0]?.createdAt;
  const normalizedSearch = searchTerm.trim();

  return (
    <Stack spacing={3.5}>
      <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" spacing={2} alignItems={{ sm: 'center' }}>
        <div>
          <Typography variant="h4">Dashboard</Typography>
          <Typography color="text.secondary">Everything in your document archive, in one place.</Typography>
        </div>
        <Button component={Link} to="/upload" variant="contained" startIcon={<UploadRoundedIcon />}>Upload document</Button>
      </Stack>

      <Grid container spacing={2}>
        <Grid size={{ xs: 12, md: 4 }}><MetricCard label="Documents" value={String(items.length)} icon={<DescriptionRoundedIcon />} /></Grid>
        <Grid size={{ xs: 12, md: 4 }}><MetricCard label="Storage used" value={formatBytes(totalSize)} icon={<StorageRoundedIcon />} /></Grid>
        <Grid size={{ xs: 12, md: 4 }}><MetricCard label="Latest upload" value={latest ? formatDate(latest) : '—'} icon={<ScheduleRoundedIcon />} /></Grid>
      </Grid>

      <Card>
        <CardContent sx={{ p: { xs: 2, sm: 3 } }}>
          <Stack spacing={2}>
            <div>
              <Typography variant="h6">Full-text search</Typography>
              <Typography variant="body2" color="text.secondary">
                Search OCR-recognized document content stored in Elasticsearch.
              </Typography>
            </div>

            <TextField
              value={searchTerm}
              onChange={(event) => setSearchTerm(event.target.value)}
              placeholder="Search for text, e.g. Hello"
              label="Search documents"
              fullWidth
              slotProps={{
                input: {
                  startAdornment: <SearchRoundedIcon sx={{ mr: 1, color: 'text.secondary' }} />,
                },
              }}
            />

            {normalizedSearch.length === 0 ? (
              <Typography variant="body2" color="text.secondary">
                Enter a term to search the OCR index.
              </Typography>
            ) : search.isLoading ? (
              <LoadingState label="Searching documents…" />
            ) : search.isError ? (
              <ErrorState message={search.error.message} onRetry={() => void search.refetch()} />
            ) : (search.data ?? []).length === 0 ? (
              <EmptyState title="No search results" description={`No OCR text matched “${normalizedSearch}”.`} />
            ) : (
              <List disablePadding>
                {(search.data ?? []).map((result) => (
                  <ListItemButton
                    key={result.id}
                    component={Link}
                    to={`/documents/${result.id}`}
                    divider
                  >
                    <ListItemText
                      primary={result.fileName}
                      secondary={`Matched OCR text for “${normalizedSearch}”`}
                    />
                  </ListItemButton>
                ))}
              </List>
            )}
          </Stack>
        </CardContent>
      </Card>

      <Card>
        <CardContent sx={{ p: { xs: 2, sm: 3 } }}>
          <Stack spacing={2.5}>
            <div>
              <Typography variant="h6">Recent documents</Typography>
              <Typography variant="body2" color="text.secondary">Files currently stored by the REST service.</Typography>
            </div>
            {items.length === 0
              ? <EmptyState title="No documents yet" description="Upload your first PDF to get started." />
              : <DocumentTable documents={items.slice(0, 10)} />}
          </Stack>
        </CardContent>
      </Card>
    </Stack>
  );
}
