import { Component, input, output, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Manifest } from '../../models/manifest.model';
import { ManifestService } from '../../services/manifest.service';

@Component({
  selector: 'app-manifest-list',
  standalone: true,
  imports: [DatePipe],
  templateUrl: './manifest-list.html',
  styleUrl: './manifest-list.css'
})
export class ManifestList {
  protected readonly manifestService = inject(ManifestService);

  readonly manifests = input<Manifest[]>([]);
  readonly selectedId = input<string>('');
  readonly select = output<string>();

  getReceivedCount(manifest: Manifest): number {
    if (!manifest.specimens) return 0;
    return manifest.specimens.filter(s => s.status === 'Received').length;
  }

  getSpecimensCount(manifest: Manifest): number {
    return manifest.specimens?.length || 0;
  }

  getDiscrepanciesCount(manifest: Manifest): number {
    if (!manifest.discrepancies) return 0;
    return manifest.discrepancies.filter(d => d.status === 'Open').length;
  }

  onSelect(id: string): void {
    this.select.emit(id);
  }
}
