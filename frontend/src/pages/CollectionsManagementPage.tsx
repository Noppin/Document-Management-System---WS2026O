import { useState } from 'react';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { Link } from 'react-router-dom';
import {
  Alert,
  Button,
  Card,
  CardActionArea,
  CardContent,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Grid2 as Grid,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import AddRoundedIcon from '@mui/icons-material/AddRounded';
import FolderRoundedIcon from '@mui/icons-material/FolderRounded';
import { EmptyState, ErrorState, LoadingState } from '../components/common/AsyncStates';
import { useCollections, useCreateCollection } from '../features/collections/queries';
import { collectionSchema, type CollectionFormValues } from '../features/collections/validation';
import { formatDate } from '../utils/format';

export function CollectionsManagementPage() {
  const collections = useCollections();
  const create = useCreateCollection();
  const [open, setOpen] = useState(false);
  const { register, handleSubmit, reset, formState: { errors } } = useForm<CollectionFormValues>({
    resolver: zodResolver(collectionSchema),
    defaultValues: { name: '' },
  });

  if (collections.isLoading) return <LoadingState label="Loading collections…" />;
  if (collections.isError) return <ErrorState message={collections.error.message} onRetry={() => void collections.refetch()} />;

  const close = () => {
    setOpen(false);
    reset();
    create.reset();
  };

  const submit = handleSubmit(async ({ name }) => {
    await create.mutateAsync(name.trim());
    close();
  });

  return (
    <Stack spacing={3.5}>
      <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" alignItems={{ sm: 'center' }} spacing={2}>
        <div>
          <Typography variant="h4">Collections</Typography>
          <Typography color="text.secondary">Group documents into meaningful workspaces.</Typography>
        </div>
        <Button variant="contained" startIcon={<AddRoundedIcon />} onClick={() => setOpen(true)}>New collection</Button>
      </Stack>

      {(collections.data?.length ?? 0) === 0 ? (
        <Card><EmptyState title="No collections yet" description="Create a collection to organize related documents." /></Card>
      ) : (
        <Grid container spacing={2.5}>
          {collections.data?.map((collection) => (
            <Grid key={collection.id} size={{ xs: 12, sm: 6, lg: 4 }}>
              <Card sx={{ height: '100%' }}>
                <CardActionArea component={Link} to={`/collections/${collection.id}`} sx={{ height: '100%' }}>
                  <CardContent sx={{ p: 3 }}>
                    <Stack spacing={2}>
                      <FolderRoundedIcon color="primary" sx={{ fontSize: 38 }} />
                      <div>
                        <Typography variant="h6">{collection.name}</Typography>
                        <Typography variant="body2" color="text.secondary">{collection.documentCount} document{collection.documentCount === 1 ? '' : 's'}</Typography>
                      </div>
                      <Typography variant="caption" color="text.secondary">Created {formatDate(collection.createdAt)}</Typography>
                    </Stack>
                  </CardContent>
                </CardActionArea>
              </Card>
            </Grid>
          ))}
        </Grid>
      )}

      <Dialog open={open} onClose={close} fullWidth maxWidth="xs">
        <Stack component="form" onSubmit={submit}>
          <DialogTitle>Create collection</DialogTitle>
          <DialogContent>
            <Typography color="text.secondary" sx={{ mb: 2 }}>Choose a name that makes the grouped documents easy to find.</Typography>
            <TextField
              autoFocus
              fullWidth
              label="Collection name"
              {...register('name')}
              error={Boolean(errors.name)}
              helperText={errors.name?.message}
            />
            {create.isError && <Alert severity="error" sx={{ mt: 2 }}>{create.error.message}</Alert>}
          </DialogContent>
          <DialogActions>
            <Button onClick={close} color="inherit">Cancel</Button>
            <Button type="submit" variant="contained" disabled={create.isPending}>Create</Button>
          </DialogActions>
        </Stack>
      </Dialog>
    </Stack>
  );
}
