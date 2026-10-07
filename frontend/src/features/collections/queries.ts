import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { assignDocument, createCollection, getCollection, getCollections, removeDocumentFromCollection } from './api';
import { documentKeys } from '../documents/queries';

export const collectionKeys = {
  all: ['collections'] as const,
  detail: (id: string) => ['collections', id] as const,
};

export function useCollections() {
  return useQuery({ queryKey: collectionKeys.all, queryFn: getCollections });
}

export function useCollection(id: string) {
  return useQuery({ queryKey: collectionKeys.detail(id), queryFn: () => getCollection(id), enabled: Boolean(id) });
}

export function useCreateCollection() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: createCollection,
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: collectionKeys.all }),
  });
}

export function useAssignDocument() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ collectionId, documentId }: { collectionId: string; documentId: string }) => assignDocument(collectionId, documentId),
    onSuccess: (_, input) => {
      void queryClient.invalidateQueries({ queryKey: collectionKeys.detail(input.collectionId) });
      void queryClient.invalidateQueries({ queryKey: collectionKeys.all });
      void queryClient.invalidateQueries({ queryKey: documentKeys.all });
    },
  });
}

export function useRemoveDocumentFromCollection() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ collectionId, documentId }: { collectionId: string; documentId: string }) => removeDocumentFromCollection(collectionId, documentId),
    onSuccess: (_, input) => {
      void queryClient.invalidateQueries({ queryKey: collectionKeys.detail(input.collectionId) });
      void queryClient.invalidateQueries({ queryKey: collectionKeys.all });
    },
  });
}
