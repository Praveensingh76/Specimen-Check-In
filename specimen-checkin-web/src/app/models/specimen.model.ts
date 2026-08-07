export interface Specimen {
  id: string;
  manifestId: string;
  specimenNumber: string;
  patientName: string;
  accessionNumber: string;
  collectionDate: string;
  receivedDate?: string;
  status: 'Pending' | 'CheckedIn' | 'Rejected';
  rejectionReason?: string;
  tenantId: string;
  checkedInBy?: string;
}
