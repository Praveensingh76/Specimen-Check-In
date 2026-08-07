import { Specimen } from './specimen.model';

export interface Manifest {
  id: string;
  manifestNumber: string;
  senderName: string;
  status: 'Created' | 'Received' | 'Completed';
  tenantId: string;
  createdAt: string;
  specimens?: Specimen[];
}
