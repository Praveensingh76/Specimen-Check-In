export type DiscrepancyType = 'Missing' | 'OffManifest';
export type DiscrepancyStatus = 'Open' | 'Resolved';

export interface Discrepancy {
  id: string;
  manifestId: string;
  specimenId?: string;
  type: DiscrepancyType;
  status: DiscrepancyStatus;
  notes?: string;
}
