export type DocumentDto = {
  id: string;
  fileName: string;
  contentType: string;
  fileSize: number;
  createdAt: string;
};

export type DocumentSearchResultDto = {
  id: string;
  fileName: string;
};
