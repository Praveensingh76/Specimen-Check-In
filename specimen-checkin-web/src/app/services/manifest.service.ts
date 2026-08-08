import { Injectable, inject, signal } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable, tap } from "rxjs";
import { Manifest } from "../models/manifest.model";
import { Specimen } from "../models/specimen.model";

@Injectable({
  providedIn: "root",
})
export class ManifestService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = "https://localhost:44379/api/manifests";

  // Shared state signals
  readonly selectedManifestId = signal<string>("");
  readonly selectedManifest = signal<Manifest | null>(null);

  getManifests(): Observable<Manifest[]> {
    return this.http.get<Manifest[]>(this.apiUrl);
  }

  getManifest(id: string): Observable<Manifest> {
    return this.http.get<Manifest>(`${this.apiUrl}/${id}`).pipe(
      tap((m) => {
        if (this.selectedManifestId() === id) {
          this.selectedManifest.set(m);
        }
      }),
    );
  }

  createManifest(manifest: Partial<Manifest>): Observable<Manifest> {
    return this.http.post<Manifest>(this.apiUrl, manifest);
  }

  receiveSpecimen(
    id: string,
    sid: string,
    receivedBy: string,
  ): Observable<Specimen> {
    return this.http
      .post<Specimen>(`${this.apiUrl}/${id}/specimens/${sid}/receive`, {
        receivedBy,
      })
      .pipe(tap(() => this.reloadSelectedManifest(id)));
  }

  flagSpecimen(
    id: string,
    sid: string,
    receivedBy: string,
    notes: string,
  ): Observable<Specimen> {
    return this.http
      .post<Specimen>(`${this.apiUrl}/${id}/specimens/${sid}/flag`, {
        receivedBy,
        notes,
      })
      .pipe(tap(() => this.reloadSelectedManifest(id)));
  }

  closeManifest(id: string): Observable<Manifest> {
    return this.http.post<Manifest>(`${this.apiUrl}/${id}/close`, {}).pipe(
      tap((m) => {
        if (this.selectedManifestId() === id) {
          this.selectedManifest.set(m);
        }
      }),
    );
  }

  selectManifest(id: string): void {
    this.selectedManifestId.set(id);
    if (!id) {
      this.selectedManifest.set(null);
      return;
    }

    this.getManifest(id).subscribe({
      next: (m) => this.selectedManifest.set(m),
      error: () => this.selectedManifest.set(null),
    });
  }

  private reloadSelectedManifest(id: string): void {
    if (this.selectedManifestId() === id) {
      this.getManifest(id).subscribe({
        next: (m) => this.selectedManifest.set(m),
      });
    }
  }
}
