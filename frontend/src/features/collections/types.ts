import type { DocumentDto } from '../documents/types';

export type CollectionDto = {
  id: string;
  name: string;
  createdAt: string;
  documentCount: number;
};

export type CollectionDetailDto = {
  id: string;
  name: string;
  createdAt: string;
  documents: DocumentDto[];
};
