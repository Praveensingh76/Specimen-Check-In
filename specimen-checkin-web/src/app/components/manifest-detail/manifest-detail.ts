import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DatePipe, CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ManifestService } from '../../services/manifest.service';
import { SpecimenService } from '../../services/specimen.service';
import { TenantService } from '../../services/tenant.service';
import { Manifest } from '../../models/manifest.model';
import { Specimen } from '../../models/specimen.model';

@Component({
  selector: 'app-manifest-detail',
  standalone: true,
  imports: [RouterLink, DatePipe, FormsModule, CommonModule],
  templateUrl: './manifest-detail.html',
  styleUrl: './manifest-detail.css'
})
export class ManifestDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly manifestService = inject(ManifestService);
  private readonly specimenService = inject(SpecimenService);
  protected readonly tenantService = inject(TenantService);

  readonly manifest = signal<Manifest | null>(null);
  readonly isLoading = signal<boolean>(false);
  readonly errorMessage = signal<string>('');
  readonly successMessage = signal<string>('');

  // Barcode quick check-in input
  readonly barcodeSearchInput = signal<string>('');

  // Filtering list
  readonly filterQuery = signal<string>('');

  // Rejection modal control
  readonly activeRejectionSpecimen = signal<Specimen | null>(null);
  readonly rejectionReason = signal<string>('');
  readonly isRejecting = signal<boolean>(false);

  // Operator ID/Name for check-in audits
  readonly operatorName = signal<string>('Lab Tech Alice');

  // Filtered specimens list
  readonly filteredSpecimens = computed(() => {
    const list = this.manifest()?.specimens || [];
    const query = this.filterQuery().toLowerCase().trim();
    if (!query) return list;

    return list.filter(s => 
      s.specimenNumber.toLowerCase().includes(query) ||
      s.patientName.toLowerCase().includes(query) ||
      s.accessionNumber.toLowerCase().includes(query)
    );
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadManifest(id);
    }
  }

  loadManifest(id: string): void {
    this.isLoading.set(true);
    this.errorMessage.set('');
    
    this.manifestService.getManifest(id).subscribe({
      next: (data) => {
        this.manifest.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMessage.set('Failed to load manifest details.');
        console.error(err);
      }
    });
  }

  checkInSpecimen(specimen: Specimen): void {
    this.clearAlerts();
    this.specimenService.checkInSpecimen(specimen.id, this.operatorName()).subscribe({
      next: (updatedSpecimen) => {
        this.showSuccess(`Specimen ${updatedSpecimen.specimenNumber} checked in successfully!`);
        this.refreshSpecimenList(updatedSpecimen);
      },
      error: (err) => {
        this.errorMessage.set(err.error || 'Failed to check in specimen.');
      }
    });
  }

  openRejectionModal(specimen: Specimen): void {
    this.clearAlerts();
    this.activeRejectionSpecimen.set(specimen);
    this.rejectionReason.set('');
  }

  closeRejectionModal(): void {
    this.activeRejectionSpecimen.set(null);
  }

  submitRejection(): void {
    const specimen = this.activeRejectionSpecimen();
    const reason = this.rejectionReason().trim();

    if (!specimen || !reason) return;

    this.isRejecting.set(true);
    this.specimenService.rejectSpecimen(specimen.id, this.operatorName(), reason).subscribe({
      next: (updatedSpecimen) => {
        this.showSuccess(`Specimen ${updatedSpecimen.specimenNumber} rejected: ${reason}`);
        this.refreshSpecimenList(updatedSpecimen);
        this.isRejecting.set(false);
        this.closeRejectionModal();
      },
      error: (err) => {
        this.isRejecting.set(false);
        this.errorMessage.set(err.error || 'Failed to reject specimen.');
        this.closeRejectionModal();
      }
    });
  }

  // Simulates scanning a barcode (e.g. typing barcode and hitting Enter)
  onBarcodeScan(): void {
    const code = this.barcodeSearchInput().trim();
    if (!code) return;

    this.clearAlerts();
    const specimensList = this.manifest()?.specimens || [];
    const found = specimensList.find(s => s.specimenNumber.toLowerCase() === code.toLowerCase());

    if (!found) {
      this.errorMessage.set(`Barcode '${code}' not found in this manifest.`);
      this.barcodeSearchInput.set('');
      return;
    }

    if (found.status === 'CheckedIn') {
      this.showSuccess(`Specimen ${found.specimenNumber} is already checked in.`);
      this.barcodeSearchInput.set('');
      return;
    }

    // Auto check-in if found
    this.checkInSpecimen(found);
    this.barcodeSearchInput.set('');
  }

  private refreshSpecimenList(updated: Specimen): void {
    const currentManifest = this.manifest();
    if (!currentManifest || !currentManifest.specimens) return;

    // Replace the updated specimen in the manifest signals
    const updatedSpecimens = currentManifest.specimens.map(s => 
      s.id === updated.id ? updated : s
    );

    // Re-calculate the manifest status based on updated list
    const allProcessed = updatedSpecimens.all(s => s.status === 'CheckedIn' || s.status === 'Rejected');
    const anyProcessed = updatedSpecimens.any(s => s.status === 'CheckedIn' || s.status === 'Rejected');
    
    let newStatus = currentManifest.status;
    if (allProcessed) {
      newStatus = 'Completed';
    } else if (anyProcessed) {
      newStatus = 'Received';
    }

    this.manifest.set({
      ...currentManifest,
      status: newStatus,
      specimens: updatedSpecimens
    });
  }

  private clearAlerts(): void {
    this.errorMessage.set('');
    this.successMessage.set('');
  }

  private showSuccess(msg: string): void {
    this.successMessage.set(msg);
    // Auto clear success message after 5 seconds
    setTimeout(() => {
      if (this.successMessage() === msg) {
        this.successMessage.set('');
      }
    }, 5000);
  }
}

// Inline JS-like extensions for Array
declare global {
  interface Array<T> {
    all(predicate: (value: T, index: number, array: T[]) => boolean): boolean;
    any(predicate: (value: T, index: number, array: T[]) => boolean): boolean;
  }
}
// Implement arrays helpers if they don't compile, although native typescript supports .every and .some
Array.prototype.all = Array.prototype.every;
Array.prototype.any = Array.prototype.some;
