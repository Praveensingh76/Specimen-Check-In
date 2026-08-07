import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { Tenant } from '../models/tenant.model';

@Injectable({
  providedIn: 'root'
})
export class TenantService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5000/api/tenants';

  // Signals for state management
  readonly tenants = signal<Tenant[]>([]);
  readonly activeTenantId = signal<string>('');

  // Computed signal to get current Tenant object
  readonly activeTenant = computed(() => 
    this.tenants().find(t => t.id === this.activeTenantId()) || null
  );

  constructor() {
    // Restore saved tenant from local storage if exists
    const savedTenantId = localStorage.getItem('activeTenantId');
    if (savedTenantId) {
      this.activeTenantId.set(savedTenantId);
    }
  }

  loadTenants(): Observable<Tenant[]> {
    return this.http.get<Tenant[]>(this.apiUrl).pipe(
      tap(tenants => {
        this.tenants.set(tenants);
        // If no tenant is selected or selected tenant isn't in list, auto-select first one
        if (tenants.length > 0 && (!this.activeTenantId() || !tenants.some(t => t.id === this.activeTenantId()))) {
          this.setActiveTenant(tenants[0].id);
        }
      })
    );
  }

  setActiveTenant(id: string): void {
    this.activeTenantId.set(id);
    localStorage.setItem('activeTenantId', id);
  }

  createTenant(tenant: Partial<Tenant>): Observable<Tenant> {
    return this.http.post<Tenant>(this.apiUrl, tenant).pipe(
      tap(newTenant => {
        this.tenants.update(list => [...list, newTenant]);
        if (!this.activeTenantId()) {
          this.setActiveTenant(newTenant.id);
        }
      })
    );
  }
}
