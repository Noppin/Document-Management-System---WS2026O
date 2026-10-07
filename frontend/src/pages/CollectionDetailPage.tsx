import { useMemo, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  Alert,
  Button,
  Card,
  CardContent,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  Typography,
} from '@mui/material';
import ArrowBackRoundedIcon from '@mui/icons-material/ArrowBackRounded';
import AddRoundedIcon from '@mui/icons-material/AddRounded';
import DeleteOutlineRoundedIcon from '@mui/icons-material/DeleteOutlineRounded';
import FolderRoundedIcon from '@mui/icons-material/FolderRounded';
import OpenInNewRoundedIcon from '@mui/icons-material/OpenInNewRounded';
import { EmptyState, ErrorState, LoadingState } from '../components/common/AsyncStates';
import { ApiError } from '../api/http';
import { useAssignDocument, useCollection, useRemoveDocumentFromCollection } from '../features/collections/queries';
import { useDocuments } from '../features/documents/queries';
import { formatBytes, formatDate } from '../utils/format';

export function CollectionDetailPage() {
  const { id = '' } = useParams();
  const collection = useCollection(id);
  const documents = useDocuments();
  const assign = useAssignDocument();
  const remove = useRemoveDocumentFromCollection();
  const [selectedDocumentId, setSelectedDocumentId] = useState('');

  const availableDocuments = useMemo(() => {
    const attached = new Set(collection.data?.documents.map((document) => document.id) ?? []);
    return (documents.data ?? []).filter((document) => !attached.has(document.id));
  }, [collection.data, documents.data]);

  if (collection.isLoading || documents.isLoading) return <LoadingState label="Loading collection…" />;
  if (collection.isError) {
    if (collection.error instanceof ApiError && collection.error.status === 404) {
      return <ErrorState message="Collection not found." />;
    }
    return <ErrorState message={collection.error.message} onRetry={() => void collection.refetch()} />;
  }
  if (documents.isError) return <ErrorState message={documents.error.message} onRetry={() => void documents.refetch()} />;
  if (!collection.data) return <ErrorState message="Collection not found." />;

  const detail = collection.data;
  const addSelected = async () => {
    if (!selectedDocumentId) return;
    await assign.mutateAsync({ collectionId: detail.id, documentId: selectedDocumentId });
    setSelectedDocumentId('');
  };

  return (
    <Stack spacing={3.5}>
      <Button component={Link} to="/collections" color="inherit" startIcon={<ArrowBackRoundedIcon />} sx={{ alignSelf: 'flex-start' }}>
        Collections
      </Button>

      <Stack direction={{ xs: 'column', md: 'row' }} justifyContent="space-between" alignItems={{ md: 'center' }} spacing={2}>
        <Stack direction="row" spacing={1.5} alignItems="center">
          <FolderRoundedIcon color="primary" sx={{ fontSize: 42 }} />
          <div>
            <Typography variant="h4">{detail.name}</Typography>
            <Typography color="text.secondary">Created {formatDate(detail.createdAt)} · {detail.documents.length} document{detail.documents.length === 1 ? '' : 's'}</Typography>
          </div>
        </Stack>
      </Stack>

      <Card>
        <CardContent sx={{ p: { xs: 2.5, sm: 3 } }}>
          <Stack spacing={2}>
            <Typography variant="h6">Add a document</Typography>
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5} alignItems={{ sm: 'center' }}>
              <FormControl sx={{ minWidth: 280, flex: 1 }} disabled={availableDocuments.length === 0}>
                <InputLabel id="document-select-label">Document</InputLabel>
                <Select
                  labelId="document-select-label"
                  label="Document"
                  value={selectedDocumentId}
                  onChange={(event) => setSelectedDocumentId(event.target.value)}
                >
                  {availableDocuments.map((document) => (
                    <MenuItem key={document.id} value={document.id}>{document.fileName}</MenuItem>
                  ))}
                </Select>
              </FormControl>
              <Button
                variant="contained"
                startIcon={<AddRoundedIcon />}
                disabled={!selectedDocumentId || assign.isPending}
                onClick={() => void addSelected()}
              >
                Add to collection
              </Button>
            </Stack>
            {availableDocuments.length === 0 && <Typography variant="body2" color="text.secondary">All available documents are already assigned.</Typography>}
            {assign.isError && <Alert severity="error">{assign.error.message}</Alert>}
          </Stack>
        </CardContent>
      </Card>

      <Card>
        <CardContent sx={{ p: { xs: 2.5, sm: 3 } }}>
          <Stack spacing={2.5}>
            <Typography variant="h6">Documents in this collection</Typography>
            {detail.documents.length === 0 ? (
              <EmptyState title="This collection is empty" description="Choose a document above to add it." />
            ) : (
              <Stack spacing={1.25}>
                {detail.documents.map((document) => (
                  <Stack
                    key={document.id}
                    direction={{ xs: 'column', sm: 'row' }}
                    justifyContent="space-between"
                    alignItems={{ sm: 'center' }}
                    spacing={1.5}
                    sx={{ p: 2, border: 1, borderColor: 'divider', borderRadius: 2.5 }}
                  >
                    <div>
                      <Typography fontWeight={700}>{document.fileName}</Typography>
                      <Typography variant="body2" color="text.secondary">{formatBytes(document.fileSize)} · {formatDate(document.createdAt)}</Typography>
                    </div>
                    <Stack direction="row" spacing={1}>
                      <Button component={Link} to={`/documents/${document.id}`} size="small" startIcon={<OpenInNewRoundedIcon />}>Open</Button>
                      <Button
                        size="small"
                        color="error"
                        startIcon={<DeleteOutlineRoundedIcon />}
                        disabled={remove.isPending}
                        onClick={() => void remove.mutateAsync({ collectionId: detail.id, documentId: document.id })}
                      >
                        Remove
                      </Button>
                    </Stack>
                  </Stack>
                ))}
              </Stack>
            )}
            {remove.isError && <Alert severity="error">{remove.error.message}</Alert>}
          </Stack>
        </CardContent>
      </Card>
    </Stack>
  );
}
