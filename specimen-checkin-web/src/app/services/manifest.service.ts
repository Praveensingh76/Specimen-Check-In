import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Manifest } from '../models/manifest.model';

@Injectable({
  providedIn: 'root'
})
export class ManifestService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5000/api/manifests';

  getManifests(): Observable<Manifest[]> {
    return this.http.get<Manifest[]>(this.apiUrl);
  }

  getManifest(id: string): Observable<Manifest> {
    return this.http.get<Manifest>(`${this.apiUrl}/${id}`);
  }

  createManifest(manifest: Partial<Manifest>): Observable<Manifest> {
    return this.http.post<Manifest>(this.apiUrl, manifest);
  }

  updateManifest(id: string, manifest: Partial<Manifest>): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, manifest);
  }

  deleteManifest(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
