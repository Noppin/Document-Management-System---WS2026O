import {
  Chip,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material';
import PictureAsPdfRoundedIcon from '@mui/icons-material/PictureAsPdfRounded';
import type { DocumentDto } from '../types';
import { formatBytes, formatDate } from '../../../utils/format';

export function DocumentTable({ documents }: { documents: DocumentDto[] }) {
  return (
    <TableContainer component={Paper} variant="outlined" sx={{ boxShadow: 'none' }}>
      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell>Document</TableCell>
            <TableCell>Size</TableCell>
            <TableCell>Uploaded</TableCell>
            <TableCell>Type</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {documents.map((document) => (
            <TableRow key={document.id} hover>
              <TableCell>
                <Typography fontWeight={700} sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <PictureAsPdfRoundedIcon color="error" fontSize="small" />
                  {document.fileName}
                </Typography>
              </TableCell>
              <TableCell>{formatBytes(document.fileSize)}</TableCell>
              <TableCell>{formatDate(document.createdAt)}</TableCell>
              <TableCell><Chip label="PDF" size="small" variant="outlined" /></TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );
}
