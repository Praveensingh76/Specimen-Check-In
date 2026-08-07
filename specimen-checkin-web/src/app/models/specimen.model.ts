export type SpecimenStatus = 'Pending' | 'Received' | 'Flagged';

export interface Specimen {
  id: string;
  manifestId: string;
  code: string;
  patient: string;
  site: string;
  provider: string;
  status: SpecimenStatus;
  receivedBy?: string;
  receivedAt?: string;
}
