import { Specimen } from './specimen.model';
import { Discrepancy } from './discrepancy.model';

export type ManifestStatus = 'Open' | 'Closed' | 'ClosedWithDiscrepancy';

export interface Manifest {
  id: string;
  labId: string;
  code: string;
  status: ManifestStatus;
  sentAt: string;
  sourceClinic: string;
  specimens?: Specimen[];
  discrepancies?: Discrepancy[];
}
