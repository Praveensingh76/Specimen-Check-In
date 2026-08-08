import { Injectable, signal, computed, inject } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable, tap } from "rxjs";
import { Lab } from "../models/lab.model";

@Injectable({
  providedIn: "root",
})
export class LabService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = "https://localhost:44379/api/labs";

  // State signals
  readonly labs = signal<Lab[]>([]);
  readonly activeLabId = signal<string>("");

  // Computed signal to get current active Lab
  readonly activeLab = computed(
    () => this.labs().find((l) => l.id === this.activeLabId()) || null,
  );

  constructor() {
    // Restore saved lab context from local storage if present
    const savedLabId = localStorage.getItem("activeLabId");
    if (savedLabId) {
      this.activeLabId.set(savedLabId);
    }
  }

  loadLabs(): Observable<Lab[]> {
    return this.http.get<Lab[]>(this.apiUrl).pipe(
      tap((labs) => {
        this.labs.set(labs);
        // Default to first lab if none is active or selected isn't in lists
        if (
          labs.length > 0 &&
          (!this.activeLabId() ||
            !labs.some((l) => l.id === this.activeLabId()))
        ) {
          this.setActiveLab(labs[0].id);
        }
      }),
    );
  }

  setActiveLab(id: string): void {
    this.activeLabId.set(id);
    localStorage.setItem("activeLabId", id);
  }
}
