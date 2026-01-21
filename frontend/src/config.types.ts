export interface RuntimeConfig {
  useAsyncRead: boolean;
  useAsyncNModbus: boolean;
  allowConcurrentFrameReads: boolean;
  dataStorageBackend: string;
  minWorkerThreads: number;
}
