import { apiRequest } from '../../api/http';
import type { DocumentDto } from './types';

export function getDocuments() {
  return apiRequest<DocumentDto[]>('/documents');
}

export function getDocument(id: string) {
  return apiRequest<DocumentDto>(`/documents/${id}`);
}

export function uploadDocument(file: File) {
  const body = new FormData();
  body.append('file', file);
  return apiRequest<DocumentDto>('/documents', { method: 'POST', body });
}

export function deleteDocument(id: string) {
  return apiRequest<void>(`/documents/${id}`, { method: 'DELETE' });
}
