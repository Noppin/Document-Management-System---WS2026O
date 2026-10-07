import { useState } from 'react';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { Link } from 'react-router-dom';
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  LinearProgress,
  Stack,
  Typography,
} from '@mui/material';
import CloudUploadRoundedIcon from '@mui/icons-material/CloudUploadRounded';
import PictureAsPdfRoundedIcon from '@mui/icons-material/PictureAsPdfRounded';
import CheckCircleRoundedIcon from '@mui/icons-material/CheckCircleRounded';
import { useUploadDocument } from '../features/documents/queries';
import { uploadSchema, type UploadFormValues } from '../features/documents/validation';
import { formatBytes } from '../utils/format';

export function ValidatedUploadPage() {
  const upload = useUploadDocument();
  const [dragActive, setDragActive] = useState(false);
  const {
    handleSubmit,
    setValue,
    watch,
    formState: { errors },
  } = useForm<UploadFormValues>({ resolver: zodResolver(uploadSchema) });

  const selectedFile = watch('file');

  const chooseFile = (file?: File) => {
    if (file) setValue('file', file, { shouldDirty: true, shouldValidate: true });
  };

  const onSubmit = handleSubmit(async ({ file }) => {
    await upload.mutateAsync(file);
  });

  return (
    <Stack spacing={3.5}>
      <div>
        <Typography variant="h4">Upload document</Typography>
        <Typography color="text.secondary">Add a PDF to the archive. Files are validated before they are sent.</Typography>
      </div>

      <Card sx={{ maxWidth: 820 }}>
        {upload.isPending && <LinearProgress />}
        <CardContent sx={{ p: { xs: 2.5, sm: 4 } }}>
          <Stack component="form" onSubmit={onSubmit} spacing={3}>
            <Box
              component="label"
              onDragEnter={(event) => { event.preventDefault(); setDragActive(true); }}
              onDragOver={(event) => event.preventDefault()}
              onDragLeave={(event) => { event.preventDefault(); setDragActive(false); }}
              onDrop={(event) => {
                event.preventDefault();
                setDragActive(false);
                chooseFile(event.dataTransfer.files[0]);
              }}
              sx={{
                display: 'block',
                cursor: 'pointer',
                border: '2px dashed',
                borderColor: errors.file ? 'error.main' : dragActive ? 'primary.main' : 'divider',
                bgcolor: dragActive ? 'action.hover' : 'background.default',
                borderRadius: 3,
                p: { xs: 4, sm: 6 },
                textAlign: 'center',
                transition: '150ms ease',
              }}
            >
              <input
                hidden
                type="file"
                accept="application/pdf,.pdf"
                onChange={(event) => chooseFile(event.target.files?.[0])}
              />
              <CloudUploadRoundedIcon color="primary" sx={{ fontSize: 52 }} />
              <Typography variant="h6" sx={{ mt: 1 }}>Drop a PDF here or click to browse</Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>PDF only · maximum 20 MB</Typography>
            </Box>

            {selectedFile && (
              <Stack direction="row" spacing={1.5} alignItems="center" sx={{ p: 2, bgcolor: 'action.hover', borderRadius: 2.5 }}>
                <PictureAsPdfRoundedIcon color="error" />
                <Box sx={{ minWidth: 0, flex: 1 }}>
                  <Typography fontWeight={700} noWrap>{selectedFile.name}</Typography>
                  <Typography variant="body2" color="text.secondary">{formatBytes(selectedFile.size)}</Typography>
                </Box>
              </Stack>
            )}

            {errors.file && <Alert severity="error">{errors.file.message}</Alert>}
            {upload.isError && <Alert severity="error">{upload.error.message}</Alert>}
            {upload.isSuccess && (
              <Alert severity="success" icon={<CheckCircleRoundedIcon />}>
                Upload complete. <Button component={Link} to={`/documents/${upload.data.id}`} size="small">Open document</Button>
              </Alert>
            )}

            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
              <Button type="submit" variant="contained" disabled={upload.isPending} startIcon={<CloudUploadRoundedIcon />}>
                {upload.isPending ? 'Uploading…' : 'Upload PDF'}
              </Button>
              <Button component={Link} to="/" color="inherit">Back to dashboard</Button>
            </Stack>
          </Stack>
        </CardContent>
      </Card>
    </Stack>
  );
}
