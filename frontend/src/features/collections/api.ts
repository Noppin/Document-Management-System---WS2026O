import { apiRequest } from '../../api/http';
import type { CollectionDetailDto, CollectionDto } from './types';

export function getCollections() {
  return apiRequest<CollectionDto[]>('/collections');
}

export function getCollection(id: string) {
  return apiRequest<CollectionDetailDto>(`/collections/${id}`);
}

export function createCollection(name: string) {
  return apiRequest<CollectionDto>('/collections', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name }),
  });
}

export function assignDocument(collectionId: string, documentId: string) {
  return apiRequest<void>(`/collections/${collectionId}/documents`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ documentId }),
  });
}

export function removeDocumentFromCollection(collectionId: string, documentId: string) {
  return apiRequest<void>(`/collections/${collectionId}/documents/${documentId}`, { method: 'DELETE' });
}
