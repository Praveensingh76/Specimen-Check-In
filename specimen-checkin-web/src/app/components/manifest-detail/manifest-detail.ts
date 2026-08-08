import { Component, input, output, inject, signal, computed, effect } from '@angular/core';
import { DatePipe, CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { Manifest } from '../../models/manifest.model';
import { Specimen } from '../../models/specimen.model';
import { StatusClassPipe } from '../../pipes/status-class.pipe';
import { ManifestService } from '../../services/manifest.service';

@Component({
  selector: 'app-manifest-detail',
  standalone: true,
  imports: [DatePipe, CommonModule, ReactiveFormsModule, FormsModule, StatusClassPipe],
  templateUrl: './manifest-detail.html',
  styleUrl: './manifest-detail.css'
})
export class ManifestDetail {
  private readonly fb = inject(FormBuilder);
  protected readonly manifestService = inject(ManifestService);

  readonly manifest = input<Manifest | null>(null);
  readonly operatorName = input<string>('Lab Tech Alice');
  
  // Events
  readonly errorOccurred = output<string>();
  readonly successOccurred = output<string>();
  readonly backToList = output<void>();

  onBack(): void {
    this.backToList.emit();
  }

  // State
  readonly isFlagDialogOpen = signal<boolean>(false);
  readonly flaggingSpecimen = signal<Specimen | null>(null);
  readonly isSubmitting = signal<boolean>(false);
  
  readonly flagForm: FormGroup;

  // Stats computed from active manifest input signal
  readonly stats = computed(() => {
    const m = this.manifest();
    if (!m) return { expected: 0, received: 0, pending: 0, flagged: 0, openDiscrepancies: 0 };

    const specimens = m.specimens || [];
    const discrepancies = m.discrepancies || [];

    return {
      expected: specimens.length,
      received: specimens.filter(s => s.status === 'Received').length,
      pending: specimens.filter(s => s.status === 'Pending').length,
      flagged: specimens.filter(s => s.status === 'Flagged').length,
      openDiscrepancies: discrepancies.filter(d => d.status === 'Open').length
    };
  });

  // Reconciled flag: true if no pending specimens AND no open discrepancies
  readonly isReconciled = computed(() => {
    const s = this.stats();
    return s.expected > 0 && s.pending === 0 && s.openDiscrepancies === 0;
  });

  constructor() {
    this.flagForm = this.fb.group({
      receivedBy: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
      specimenId: ['', [Validators.required]],
      notes: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(500)]]
    });

    // Automatically sync operator name into the form
    effect(() => {
      this.flagForm.patchValue({ receivedBy: this.operatorName() });
    });
  }

  onReceiveSpecimen(specimen: Specimen): void {
    const m = this.manifest();
    if (!m) return;

    this.manifestService.receiveSpecimen(m.id, specimen.id, this.operatorName()).subscribe({
      next: () => {
        this.successOccurred.emit(`Specimen ${specimen.code} marked as Received.`);
      },
      error: (err) => {
        this.errorOccurred.emit(err.error?.detail || 'Failed to receive specimen.');
      }
    });
  }

  openFlagDialog(specimen: Specimen | null): void {
    this.flaggingSpecimen.set(specimen);
    this.flagForm.patchValue({
      receivedBy: this.operatorName(),
      specimenId: specimen ? specimen.id : '',
      notes: ''
    });
    
    // Enable/disable specimen selection dropdown
    if (specimen) {
      this.flagForm.get('specimenId')?.disable();
    } else {
      this.flagForm.get('specimenId')?.enable();
    }

    this.flagForm.markAsPristine();
    this.flagForm.markAsUntouched();
    this.isFlagDialogOpen.set(true);
  }

  closeFlagDialog(): void {
    this.isFlagDialogOpen.set(false);
    this.flaggingSpecimen.set(null);
  }

  onFlagSpecimenSubmit(): void {
    if (this.flagForm.invalid) {
      this.flagForm.markAllAsTouched();
      return;
    }

    const m = this.manifest();
    if (!m) return;

    // Use selected specimen ID from form (handles both disabled row selection and enabled top-right dropdown)
    const rawFormValue = this.flagForm.getRawValue();
    const targetSpecimenId = rawFormValue.specimenId;
    const targetSpecimen = m.specimens?.find(s => s.id === targetSpecimenId);

    if (!targetSpecimenId || !targetSpecimen) {
      this.errorOccurred.emit('Please select a valid specimen to flag.');
      return;
    }

    this.isSubmitting.set(true);

    this.manifestService.flagSpecimen(m.id, targetSpecimenId, rawFormValue.receivedBy, rawFormValue.notes).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.successOccurred.emit(`Specimen ${targetSpecimen.code} marked as Flagged (Discrepancy raised).`);
        this.closeFlagDialog();
      },
      error: (err) => {
        this.isSubmitting.set(false);
        this.errorOccurred.emit(err.error?.detail || 'Failed to flag specimen.');
        this.closeFlagDialog();
      }
    });
  }

  onCloseManifest(): void {
    const m = this.manifest();
    if (!m) return;

    this.manifestService.closeManifest(m.id).subscribe({
      next: (closedManifest) => {
        this.successOccurred.emit(`Manifest ${closedManifest.code} successfully Reconciled & Closed.`);
      },
      error: (err) => {
        this.errorOccurred.emit(err.error?.detail || 'Failed to close manifest.');
      }
    });
  }
}
