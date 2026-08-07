import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LabService } from '../../services/lab.service';
import { ManifestService } from '../../services/manifest.service';
import { Manifest } from '../../models/manifest.model';
import { ManifestList } from '../manifest-list/manifest-list';
import { ManifestDetail } from '../manifest-detail/manifest-detail';

@Component({
  selector: 'app-check-in-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, ManifestList, ManifestDetail],
  templateUrl: './check-in-dashboard.html',
  styleUrl: './check-in-dashboard.css'
})
export class CheckInDashboard implements OnInit {
  protected readonly labService = inject(LabService);
  protected readonly manifestService = inject(ManifestService);

  // States
  readonly manifests = signal<Manifest[]>([]);
  readonly isLoading = signal<boolean>(false);
  readonly errorMessage = signal<string>('');
  
  // Notification states
  readonly errorNotification = signal<string>('');
  readonly successNotification = signal<string>('');

  // Operator ID
  readonly operatorName = signal<string>('Lab Tech Alice');

  ngOnInit(): void {
    this.isLoading.set(true);
    // Load labs and then fetch manifests for default active lab
    this.labService.loadLabs().subscribe({
      next: () => {
        this.fetchManifests();
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMessage.set('Failed to connect to the backend API. Please ensure the API server is running.');
        console.error(err);
      }
    });
  }

  onLabChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    if (select.value) {
      this.labService.setActiveLab(select.value);
      this.manifestService.selectManifest(''); // Clear selection
      this.fetchManifests();
    }
  }

  fetchManifests(): void {
    this.isLoading.set(true);
    this.errorMessage.set('');
    this.clearNotifications();

    this.manifestService.getManifests().subscribe({
      next: (data) => {
        this.manifests.set(data);
        this.isLoading.set(false);

        // Auto-select first manifest if none is currently selected
        if (data.length > 0) {
          const currentSelected = this.manifestService.selectedManifestId();
          if (!currentSelected || !data.some(m => m.id === currentSelected)) {
            this.manifestService.selectManifest(data[0].id);
          } else {
            // Refresh details of already selected manifest
            this.manifestService.selectManifest(currentSelected);
          }
        } else {
          this.manifestService.selectManifest('');
        }
      },
      error: (err) => {
        this.manifests.set([]);
        this.manifestService.selectManifest('');
        this.isLoading.set(false);
        this.errorMessage.set('Could not load manifests for the selected lab. Ensure the lab is properly seeded.');
        console.error(err);
      }
    });
  }

  onSelectManifest(id: string): void {
    this.clearNotifications();
    this.manifestService.selectManifest(id);
  }

  onErrorNotification(msg: string): void {
    this.errorNotification.set(msg);
    setTimeout(() => {
      if (this.errorNotification() === msg) {
        this.errorNotification.set('');
      }
    }, 5000);
  }

  onSuccessNotification(msg: string): void {
    this.successNotification.set(msg);
    // When checks occur, refresh list to show updated counts/states
    this.refreshManifestList();
    setTimeout(() => {
      if (this.successNotification() === msg) {
        this.successNotification.set('');
      }
    }, 5000);
  }

  private refreshManifestList(): void {
    // Silently updates manifest list metadata counts
    this.manifestService.getManifests().subscribe({
      next: (data) => {
        this.manifests.set(data);
      }
    });
  }

  private clearNotifications(): void {
    this.errorNotification.set('');
    this.successNotification.set('');
  }
}
