import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { LabService } from './lab.service';

export const labInterceptor: HttpInterceptorFn = (req, next) => {
  const labService = inject(LabService);
  const activeLabId = labService.activeLabId();

  // Inject active X-Lab-Id header for all backend API calls
  if (activeLabId && req.url.startsWith('http://localhost:5000/api')) {
    const modifiedReq = req.clone({
      setHeaders: {
        'X-Lab-Id': activeLabId
      }
    });
    return next(modifiedReq);
  }

  return next(req);
};
