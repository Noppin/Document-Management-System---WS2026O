import { useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import {
  Button,
  Card,
  CardContent,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  Grid2 as Grid,
  Stack,
  Typography,
} from '@mui/material';
import ArrowBackRoundedIcon from '@mui/icons-material/ArrowBackRounded';
import DeleteOutlineRoundedIcon from '@mui/icons-material/DeleteOutlineRounded';
import DownloadRoundedIcon from '@mui/icons-material/DownloadRounded';
import PictureAsPdfRoundedIcon from '@mui/icons-material/PictureAsPdfRounded';
import { ErrorState, LoadingState } from '../components/common/AsyncStates';
import { ApiError } from '../api/http';
import { documentContentUrl } from '../features/documents/api';
import { useDeleteDocument, useDocument } from '../features/documents/queries';
import { formatBytes, formatDate } from '../utils/format';
import { downloadDocument } from '../features/documents/api';

export function DocumentDetailPage() {
  const { id = '' } = useParams();
  const navigate = useNavigate();
  const document = useDocument(id);
  const remove = useDeleteDocument();
  const [confirmOpen, setConfirmOpen] = useState(false);

  if (document.isLoading) return <LoadingState label="Loading document…" />;
  if (document.isError) {
    if (document.error instanceof ApiError && document.error.status === 404) {
      return <ErrorState message="Document not found." />;
    }
    return <ErrorState message={document.error.message} onRetry={() => void document.refetch()} />;
  }
  if (!document.data) return <ErrorState message="Document not found." />;

  const item = document.data;
  const deleteCurrent = async () => {
    await remove.mutateAsync(item.id);
    navigate('/');
  };

  return (
    <Stack spacing={3.5}>
      <Stack direction='row' alignItems='center' spacing={1}>
        <Button component={Link} to='/' color='inherit' startIcon={<ArrowBackRoundedIcon />}>
          Dashboard
        </Button>
      </Stack>

      <Stack
        direction={{ xs: 'column', md: 'row' }}
        justifyContent='space-between'
        alignItems={{ md: 'center' }}
        spacing={2}
      >
        <div>
          <Typography variant='h4' sx={{ overflowWrap: 'anywhere' }}>
            {item.fileName}
          </Typography>
          <Typography color='text.secondary'>Document details and file actions.</Typography>
        </div>
        <Stack direction='row' spacing={1.5}>
          <Button
            variant='contained'
            startIcon={<DownloadRoundedIcon />}
            onClick={() => downloadDocument(item.id, item.fileName)}
          >
            Download
          </Button>
          <Button
            color='error'
            variant='outlined'
            startIcon={<DeleteOutlineRoundedIcon />}
            onClick={() => setConfirmOpen(true)}
          >
            Delete
          </Button>
        </Stack>
      </Stack>

      <Grid container spacing={3}>
        <Grid size={{ xs: 12, lg: 8 }}>
          <Card>
            <CardContent sx={{ p: { xs: 3, sm: 4 } }}>
              <Stack spacing={3}>
                <Stack direction='row' spacing={2} alignItems='center'>
                  <PictureAsPdfRoundedIcon color='error' sx={{ fontSize: 52 }} />
                  <div>
                    <Typography variant='h6'>Stored PDF</Typography>
                    <Chip label={item.contentType} size='small' variant='outlined' />
                  </div>
                </Stack>
                <Divider />
                <Grid container spacing={3}>
                  <Grid size={{ xs: 12, sm: 6 }}>
                    <Typography variant='caption' color='text.secondary'>
                      FILE SIZE
                    </Typography>
                    <Typography fontWeight={700}>{formatBytes(item.fileSize)}</Typography>
                  </Grid>
                  <Grid size={{ xs: 12, sm: 6 }}>
                    <Typography variant='caption' color='text.secondary'>
                      UPLOADED
                    </Typography>
                    <Typography fontWeight={700}>{formatDate(item.createdAt)}</Typography>
                  </Grid>
                  <Grid size={{ xs: 12 }}>
                    <Typography variant='caption' color='text.secondary'>
                      DOCUMENT ID
                    </Typography>
                    <Typography fontFamily='monospace' sx={{ overflowWrap: 'anywhere' }}>
                      {item.id}
                    </Typography>
                  </Grid>
                </Grid>
              </Stack>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      <Dialog open={confirmOpen} onClose={() => setConfirmOpen(false)}>
        <DialogTitle>Delete document?</DialogTitle>
        <DialogContent>
          <Typography>
            This permanently removes <strong>{item.fileName}</strong> from the archive.
          </Typography>
          {remove.isError && (
            <Typography color='error' sx={{ mt: 2 }}>
              {remove.error.message}
            </Typography>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setConfirmOpen(false)} color='inherit'>
            Cancel
          </Button>
          <Button
            onClick={() => void deleteCurrent()}
            color='error'
            variant='contained'
            disabled={remove.isPending}
          >
            Delete
          </Button>
        </DialogActions>
      </Dialog>
    </Stack>
  );
}
