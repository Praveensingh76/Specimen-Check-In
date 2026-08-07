import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { TenantService } from './tenant.service';

export const tenantInterceptor: HttpInterceptorFn = (req, next) => {
  const tenantService = inject(TenantService);
  const activeTenantId = tenantService.activeTenantId();

  // Inject header if activeTenantId is set and it's an API request
  if (activeTenantId && req.url.startsWith('http://localhost:5000/api')) {
    const modifiedReq = req.clone({
      setHeaders: {
        'X-Tenant-ID': activeTenantId
      }
    });
    return next(modifiedReq);
  }

  return next(req);
};
