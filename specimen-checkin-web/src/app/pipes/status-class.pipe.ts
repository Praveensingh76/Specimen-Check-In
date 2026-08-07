import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'statusClass',
  standalone: true
})
export class StatusClassPipe implements PipeTransform {
  transform(status: string | undefined): string {
    if (!status) return 'badge';

    switch (status) {
      case 'Pending':
      case 'Open':
        return 'badge badge-pending';
      case 'Received':
      case 'Closed':
      case 'Resolved':
        return 'badge badge-success';
      case 'Flagged':
      case 'ClosedWithDiscrepancy':
      case 'Missing':
        return 'badge badge-danger';
      case 'OffManifest':
        return 'badge badge-info';
      default:
        return 'badge';
    }
  }
}
