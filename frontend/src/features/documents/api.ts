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

export function documentContentUrl(id: string) {
  return `/api/documents/${id}/content`;
}

export async function downloadDocument(id: string, fileName: string): Promise<void> {
  const response = await fetch(`/api/documents/${id}/content`);

  if (!response.ok) {
    throw new Error('Failed to download document.');
  }

  const blob = await response.blob();
  const url = window.URL.createObjectURL(blob);

  const link = document.createElement('a');

  link.href = url;
  link.download = fileName;

  document.body.appendChild(link);
  link.click();

  link.remove();
  window.URL.revokeObjectURL(url);
}
