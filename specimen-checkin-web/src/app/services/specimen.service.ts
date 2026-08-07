import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Specimen } from '../models/specimen.model';

@Injectable({
  providedIn: 'root'
})
export class SpecimenService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5000/api/specimens';

  getSpecimens(): Observable<Specimen[]> {
    return this.http.get<Specimen[]>(this.apiUrl);
  }

  getSpecimen(id: string): Observable<Specimen> {
    return this.http.get<Specimen>(`${this.apiUrl}/${id}`);
  }

  createSpecimen(specimen: Partial<Specimen>): Observable<Specimen> {
    return this.http.post<Specimen>(this.apiUrl, specimen);
  }

  checkInSpecimen(id: string, checkedInBy: string): Observable<Specimen> {
    return this.http.post<Specimen>(`${this.apiUrl}/${id}/checkin`, { checkedInBy });
  }

  rejectSpecimen(id: string, checkedInBy: string, reason: string): Observable<Specimen> {
    return this.http.post<Specimen>(`${this.apiUrl}/${id}/reject`, { checkedInBy, reason });
  }
}
