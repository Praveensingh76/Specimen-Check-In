import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { ManifestService } from '../../services/manifest.service';
import { TenantService } from '../../services/tenant.service';
import { Manifest } from '../../models/manifest.model';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [RouterLink, DatePipe],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css'
})
export class Dashboard implements OnInit {
  private readonly manifestService = inject(ManifestService);
  protected readonly tenantService = inject(TenantService);

  readonly manifests = signal<Manifest[]>([]);
  readonly isLoading = signal<boolean>(false);
  readonly errorMessage = signal<string>('');

  // Stats computed from signals
  readonly totalManifestsCount = signal<number>(0);
  readonly pendingSpecimensCount = signal<number>(0);
  readonly checkedInSpecimensCount = signal<number>(0);

  ngOnInit(): void {
    // Reload manifests when active tenant changes
    this.tenantService.loadTenants().subscribe({
      next: () => this.fetchManifests(),
      error: () => this.errorMessage.set('Failed to load tenants.')
    });
  }

  onTenantChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    if (select.value) {
      this.tenantService.setActiveTenant(select.value);
      this.fetchManifests();
    }
  }

  fetchManifests(): void {
    this.isLoading.set(true);
    this.errorMessage.set('');
    
    this.manifestService.getManifests().subscribe({
      next: (data) => {
        this.manifests.set(data);
        this.calculateStats(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.manifests.set([]);
        this.calculateStats([]);
        this.isLoading.set(false);
        this.errorMessage.set('Error loading manifests. Ensure API is running and tenant header is valid.');
        console.error(err);
      }
    });
  }

  private calculateStats(list: Manifest[]): void {
    this.totalManifestsCount.set(list.length);
    
    let pending = 0;
    let checkedIn = 0;
    
    list.forEach(m => {
      if (m.specimens) {
        m.specimens.forEach(s => {
          if (s.status === 'Pending') pending++;
          if (s.status === 'CheckedIn') checkedIn++;
        });
      }
    });

    this.pendingSpecimensCount.set(pending);
    this.checkedInSpecimensCount.set(checkedIn);
  }

  getProcessedPercentage(manifest: Manifest): number {
    if (!manifest.specimens || manifest.specimens.length === 0) return 0;
    const processed = manifest.specimens.filter(s => s.status !== 'Pending').length;
    return Math.round((processed / manifest.specimens.length) * 100);
  }
}
