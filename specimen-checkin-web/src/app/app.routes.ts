import { Routes } from '@angular/router';
import { CheckInDashboard } from './components/check-in-dashboard/check-in-dashboard';

export const routes: Routes = [
  { path: '', component: CheckInDashboard },
  { path: '**', redirectTo: '' }
];
