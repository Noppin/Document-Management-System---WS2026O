import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { deleteDocument, getDocument, getDocuments, uploadDocument } from './api';

export const documentKeys = {
  all: ['documents'] as const,
  detail: (id: string) => ['documents', id] as const,
};

export function useDocuments() {
  return useQuery({ queryKey: documentKeys.all, queryFn: getDocuments });
}

export function useDocument(id: string) {
  return useQuery({ queryKey: documentKeys.detail(id), queryFn: () => getDocument(id), enabled: Boolean(id) });
}

export function useUploadDocument() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: uploadDocument,
    onSuccess: (document) => {
      queryClient.setQueryData(documentKeys.detail(document.id), document);
      void queryClient.invalidateQueries({ queryKey: documentKeys.all });
    },
  });
}

export function useDeleteDocument() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: deleteDocument,
    onSuccess: (_, id) => {
      queryClient.removeQueries({ queryKey: documentKeys.detail(id) });
      void queryClient.invalidateQueries({ queryKey: documentKeys.all });
    },
  });
}
