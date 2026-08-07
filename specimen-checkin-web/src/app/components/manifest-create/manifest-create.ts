import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormBuilder, FormGroup, FormArray, Validators, ReactiveFormsModule } from '@angular/forms';
import { ManifestService } from '../../services/manifest.service';
import { TenantService } from '../../services/tenant.service';

@Component({
  selector: 'app-manifest-create',
  standalone: true,
  imports: [RouterLink, ReactiveFormsModule],
  templateUrl: './manifest-create.html',
  styleUrl: './manifest-create.css'
})
export class ManifestCreate {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly manifestService = inject(ManifestService);
  protected readonly tenantService = inject(TenantService);

  readonly manifestForm: FormGroup;
  readonly isLoading = signal<boolean>(false);
  readonly errorMessage = signal<string>('');

  constructor() {
    // Generate a default manifest number based on timestamp for convenience
    const randomSuffix = Math.floor(1000 + Math.random() * 9000);
    const defaultManifestNo = `MNF-${new Date().getFullYear()}-${randomSuffix}`;

    this.manifestForm = this.fb.group({
      manifestNumber: [defaultManifestNo, [Validators.required, Validators.pattern(/^MNF-\d{4}-\d{4,8}$/)]],
      senderName: ['', [Validators.required, Validators.minLength(3)]],
      specimens: this.fb.array([])
    });

    // Add at least one specimen field by default
    this.addSpecimen();
  }

  get specimens(): FormArray {
    return this.manifestForm.get('specimens') as FormArray;
  }

  createSpecimenFormGroup(): FormGroup {
    // Default collection date to now (formatted for datetime-local input)
    const now = new Date();
    now.setMinutes(now.getMinutes() - now.getTimezoneOffset());
    const formattedDate = now.toISOString().slice(0, 16);

    // Generate a default specimen barcode SPC-XXXXXX
    const randomBarcode = `SPC-${Math.floor(100000 + Math.random() * 900000)}`;

    return this.fb.group({
      specimenNumber: [randomBarcode, [Validators.required, Validators.minLength(3)]],
      patientName: ['', [Validators.required, Validators.minLength(2)]],
      accessionNumber: [`ACC-${Math.floor(100 + Math.random() * 900)}`, [Validators.required]],
      collectionDate: [formattedDate, [Validators.required]]
    });
  }

  addSpecimen(): void {
    this.specimens.push(this.createSpecimenFormGroup());
  }

  removeSpecimen(index: number): void {
    if (this.specimens.length > 1) {
      this.specimens.removeAt(index);
    } else {
      this.errorMessage.set('At least one specimen is required per manifest.');
      setTimeout(() => this.errorMessage.set(''), 3000);
    }
  }

  onSubmit(): void {
    if (this.manifestForm.invalid) {
      this.manifestForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set('');

    const formValue = this.manifestForm.value;
    
    this.manifestService.createManifest(formValue).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.router.navigate(['/']);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMessage.set(err.error || 'Failed to create manifest. Ensure manifest and specimen numbers are globally unique.');
        console.error(err);
      }
    });
  }
}
